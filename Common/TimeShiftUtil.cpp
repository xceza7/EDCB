#include "stdafx.h"
#include "TimeShiftUtil.h"
#include "PathUtil.h"

CTimeShiftUtil::CTimeShiftUtil(void)
	: readFile(NULL, fclose)
	, seekFile(NULL, fclose)
{
	this->PCR_PID = 0xFFFF;
	this->fileMode = FALSE;
	this->seekJitter = 1;
	this->currentFilePos = 0;
}


CTimeShiftUtil::~CTimeShiftUtil(void)
{
	StopTimeShift();

	NWPLAY_PLAY_INFO val = {};
	Send(&val);
}

BOOL CTimeShiftUtil::Send(
	NWPLAY_PLAY_INFO* val
	)
{
	CBlockLock lock(&this->utilLock);
	CBlockLock lock2(&this->ioLock);

	//送信先を設定する
	WCHAR ip[64];
	swprintf_s(ip, L"%d.%d.%d.%d", val->ip >> 24, val->ip >> 16 & 0xFF, val->ip >> 8 & 0xFF, val->ip & 0xFF);

	for( int tcp = 0; tcp < 2; tcp++ ){
		CSendNW* sendNW = (tcp ? (CSendNW*)&this->sendTcp : (CSendNW*)&this->sendUdp);
		if( this->sendIP[tcp].empty() == false && ((tcp ? val->tcp : val->udp) == 0 || this->sendIP[tcp] != ip) ){
			this->sendIP[tcp].clear();
			sendNW->StopSend();
			sendNW->UnInitialize();
			CloseHandle(this->portMutex[tcp]);
		}
		if( this->sendIP[tcp].empty() == false || (tcp ? val->tcp : val->udp) == 0 ){
			continue;
		}
		DWORD port = (tcp ? 2230 : 1234);
		wstring mutexKey;
		for( int i = 0; i < 100; i++, port++ ){
			Format(mutexKey, L"%ls%d_%d", (tcp ? MUTEX_TCP_PORT_NAME : MUTEX_UDP_PORT_NAME), val->ip, port);
			this->portMutex[tcp] = CreateMutex(NULL, FALSE, mutexKey.c_str());
			if( this->portMutex[tcp] ){
				if( GetLastError() != ERROR_ALREADY_EXISTS ){
					break;
				}
				CloseHandle(this->portMutex[tcp]);
				this->portMutex[tcp] = NULL;
			}
		}
		if( this->portMutex[tcp] ){
			OutputDebugString((mutexKey + L"\r\n").c_str());
			sendNW->Initialize();
			sendNW->AddSendAddr(ip, port, false);
			sendNW->StartSend();
			this->sendIP[tcp] = ip;
		}
	}
	return TRUE;
}

BOOL CTimeShiftUtil::OpenTimeShift(
	LPCWSTR filePath_,
	BOOL fileMode_
	)
{
	CBlockLock lock(&this->utilLock);

	StopTimeShift();

	this->PCR_PID = 0xFFFF;
	if( UtilFileExists(filePath_).first == false ){
		return FALSE;
	}

	this->filePath = filePath_;
	this->fileMode = fileMode_;
	this->seekJitter = GetTickCount() / 100 % 8 + 1;
	this->currentFilePos = 0;

	return TRUE;
}

BOOL CTimeShiftUtil::StartTimeShift()
{
	CBlockLock lock(&this->utilLock);

	if( this->filePath.size() == 0 ){
		return FALSE;
	}else{
		if( this->readThread.joinable() == false ){
			//受信スレッド起動
			this->readStopFlag = false;
			this->readThread = thread_(ReadThread, this);
		}
	}

	return TRUE;
}

BOOL CTimeShiftUtil::StopTimeShift()
{
	CBlockLock lock(&this->utilLock);

	if( this->readThread.joinable() ){
		this->readStopFlag = true;
		this->readThread.join();
	}
	this->seekFile.reset();
	this->readFile.reset();
	return TRUE;
}

void CTimeShiftUtil::ReadThread(CTimeShiftUtil* sys)
{
	BYTE buff[188*256];
	CPacketInit packetInit;

	{
		CBlockLock lock(&sys->ioLock);
		sys->readFile.reset(UtilOpenFile(sys->filePath, UTIL_SHARED_READ | UTIL_F_SEQUENTIAL));
		if( !sys->readFile ){
			return;
		}
		sys->seekFile.reset(UtilOpenFile(sys->filePath, UTIL_SHARED_READ));
		if( !sys->seekFile ){
			sys->readFile.reset();
			return;
		}
	}

	__int64 initTime = -1;
	__int64 base = -1;
	DWORD initTick = 0;
	vector<WORD> pcrPidList;
	DWORD errCount = 0;

	for(;;){
		{
			__int64 wait = 0;
			if( base >= 0 ){
				//レート調整
				wait = ((base + 0x200000000LL - initTime) & 0x1FFFFFFFFLL) / 90 - (GetTickCount() - initTick);
				base = -1;
			}else if( errCount > 0 ){
				//終端監視中
				wait = 200;
			}
			for( ; wait > 0 && sys->readStopFlag == false; wait -= 20 ){
				Sleep(20);
			}
			if( sys->readStopFlag ){
				break;
			}
		}
		CBlockLock lock(&sys->ioLock);

		__int64 pos = _ftelli64(sys->readFile.get());
		if( pos < 0 ){
			break;
		}
		if( pos != sys->currentFilePos ){
			//シークされた
			if( sys->currentFilePos >= sys->GetAvailableFileSize() ){
				//有効なデータの終端に達した
				if( sys->fileMode || ++errCount > 50 ){
					break;
				}
				continue;
			}
			if( _fseeki64(sys->readFile.get(), sys->currentFilePos, SEEK_SET) != 0 ){
				break;
			}
			packetInit.ClearBuff();
			initTime = -1;
		}
		DWORD readSize = (DWORD)fread(buff, 1, sizeof(buff), sys->readFile.get());
		if( readSize < (sys->fileMode ? 1 : sizeof(buff)) ){
			//ファイル終端に達した
			if( sys->fileMode || ++errCount > 50 ){
				break;
			}
			if( _fseeki64(sys->readFile.get(), sys->currentFilePos, SEEK_SET) != 0 ){
				break;
			}
			continue;
		}
		BYTE* data;
		DWORD dataSize;
		if( packetInit.GetTSData(buff, readSize, &data, &dataSize) == FALSE || dataSize <= 0 ){
			if( sys->fileMode == FALSE && sys->currentFilePos + (__int64)sizeof(buff) > sys->GetAvailableFileSize() ){
				//無効なデータ領域を読んでいる可能性がある
				if( ++errCount > 50 ){
					break;
				}
				if( _fseeki64(sys->readFile.get(), sys->currentFilePos, SEEK_SET) != 0 ){
					break;
				}
			}else{
				//不正なデータを読み飛ばす
				sys->currentFilePos += readSize;
				errCount = 0;
			}
			continue;
		}
		sys->currentFilePos += readSize;
		errCount = 0;

		for( DWORD i = 0; i < dataSize; i += 188 ){
			CTSPacketUtil packet;
			if( packet.Set188TS(data + i, 188) ){
				if( packet.adaptation_field_length > 0 && packet.PCR_flag == 1 ){
					//最初に3回PCRが出現したPIDをPCR_PIDとする
					//PCR_PIDが現れることなく5回別のPCRが出現すればPCR_PIDを変更する
					if( packet.PID != sys->PCR_PID ){
						pcrPidList.push_back(packet.PID);
						if( std::count(pcrPidList.begin(), pcrPidList.end(), packet.PID) >= (sys->PCR_PID == 0xFFFF ? 3 : 5) ){
							sys->PCR_PID = packet.PID;
							initTime = -1;
						}
					}
					if( packet.PID == sys->PCR_PID ){
						pcrPidList.clear();
						base = packet.program_clock_reference_base;
						if( initTime < 0 ){
							initTime = base;
							initTick = GetTickCount();
						}
					}
				}
			}
		}
		sys->sendUdp.AddSendData(data, dataSize);
		sys->sendTcp.AddSendData(data, dataSize);
	}

	CBlockLock lock(&sys->ioLock);
	sys->seekFile.reset();
	sys->readFile.reset();

	if( sys->readStopFlag == false ){
		return;
	}
	//無効PAT送って次回送信時にリセットされるようにする
	std::fill_n(buff, sizeof(buff), 0xFF);
	CCreatePATPacket patUtil;
	patUtil.SetParam(1, vector<pair<WORD, WORD>>());
	BYTE* patBuff;
	DWORD patSize=0;
	patUtil.GetPacket(&patBuff, &patSize);

	std::copy(patBuff, patBuff + patSize, buff);
	for( DWORD i=patSize; i+3<sizeof(buff); i+=188 ){
		buff[i] = 0x47;
		buff[i+1] = 0x1F;
		buff[i+2] = 0xFF;
		buff[i+3] = 0x10;
	}

	sys->sendUdp.AddSendData(buff, sizeof(buff));
	sys->sendTcp.AddSendData(buff, sizeof(buff));
}

static BOOL IsDataAvailable(FILE* fp, __int64 pos, CPacketInit* packetInit)
{
	if( _fseeki64(fp, pos, SEEK_SET) == 0 ){
		BYTE buff[188 * 16];
		DWORD readSize = (DWORD)fread(buff, 1, sizeof(buff), fp);
		if( readSize > 0 ){
			packetInit->ClearBuff();
			BYTE* data;
			DWORD dataSize;
			if( packetInit->GetTSData(buff, readSize, &data, &dataSize) && dataSize > 0 ){
				return TRUE;
			}
		}
	}
	return FALSE;
}

__int64 CTimeShiftUtil::GetAvailableFileSize() const
{
	if( this->filePath.empty() == false ){
		std::unique_ptr<FILE, decltype(&fclose)> tmpFile(NULL, fclose);
		FILE* fp = this->seekFile.get();
		if( fp == NULL ){
			tmpFile.reset(UtilOpenFile(this->filePath, UTIL_SHARED_READ | UTIL_SH_DELETE));
			fp = tmpFile.get();
		}
		__int64 fileSize = -1;
		if( fp && _fseeki64(fp, 0, SEEK_END) == 0 ){
			fileSize = _ftelli64(fp);
		}
		if( this->fileMode ){
			//単純にファイルサイズを返す
			if( fileSize >= 0 ){
				return fileSize;
			}
		}else{
			//有効なデータのある範囲を調べる
			if( fileSize >= 188 * 16 * 8 ){
				CPacketInit packetInit;
				if( IsDataAvailable(fp, fileSize - 188 * 16 * this->seekJitter, &packetInit) == FALSE ){
					//終端部分が無効なので有効なデータの境目を探す
					//seekJitterは調査箇所がたまたま壊れている場合への対処
					__int64 range = fileSize - 188 * 16 * this->seekJitter;
					__int64 pos = range / 2 / 188 * 188;
					//ここは頻繁に呼ばれると高負荷に見えるが、ファイルキャッシュがよく効く条件なのでさほどでもない
					for( ; range > 256 * 1024; range /= 2 ){
						if( IsDataAvailable(fp, pos, &packetInit) ){
							pos += range / 4 / 188 * 188;
						}else{
							pos -= range / 4 / 188 * 188;
						}
					}
					//安定のため有効なデータの境目からさらに512KBだけ手前にする
					fileSize = max(pos - range / 2 - 512 * 1024, 0LL);
				}
				return fileSize;
			}
		}
	}
	return 0;
}

void CTimeShiftUtil::GetFilePos(__int64* filePos, __int64* fileSize)
{
	CBlockLock lock(&this->utilLock);
	CBlockLock lock2(&this->ioLock);

	if( filePos != NULL ){
		*filePos = this->currentFilePos;
	}
	if( fileSize != NULL ){
		*fileSize = GetAvailableFileSize();
	}
}

void CTimeShiftUtil::SetFilePos(__int64 filePos)
{
	CBlockLock lock(&this->utilLock);
	CBlockLock lock2(&this->ioLock);

	this->currentFilePos = filePos;
}

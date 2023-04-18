using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp
{
    public class DlpSyncContext
    {
        public DlpConnection Connection { get; }

        private ReadSysInfoSystemResponse? _sysInfo;
        private ReadSysInfoDlpResponse? _dlpInfo;
        private ReadUserInfoResponse? _userInfo;

        public DlpSyncContext(DlpConnection connection)
        {
            Connection = connection;
        }

        public async Task<ReadSysInfoSystemResponse> GetSysInfo(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (_sysInfo is null || forceRefresh)
            {
                (_sysInfo, _dlpInfo) = await Connection.ReadSysInfoAsync(new() {
                    HostDlpVersionMajor = 1,
                    HostDlpVersionMinor = 1
                }, cancellationToken).ConfigureAwait(false);
            }

            return _sysInfo;
        }

        public async Task<ReadSysInfoDlpResponse> GetDlpInfo(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (_dlpInfo is null || forceRefresh)
            {
                (_sysInfo, _dlpInfo) = await Connection.ReadSysInfoAsync(new() {
                    HostDlpVersionMajor = 1,
                    HostDlpVersionMinor = 1
                }, cancellationToken).ConfigureAwait(false);
            }

            return _dlpInfo;
        }

        public async Task<ReadUserInfoResponse> GetUserInfoAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (_userInfo is null || forceRefresh)
            {
                _userInfo = await Connection.ReadUserInfoAsync(cancellationToken).ConfigureAwait(false);
            }

            return _userInfo;
        }
    }
}
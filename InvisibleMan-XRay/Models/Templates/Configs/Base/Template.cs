using System.Linq;
using Newtonsoft.Json.Linq;

namespace InvisibleManXRay.Models.Templates.Configs
{
    using Services;
    using Utilities;
    using Values;

    public abstract class Template
    {
        private V2Ray v2Ray;
        private readonly string[] removedNetworks = new[] {
            Global.StreamNetwork.H2,
            "h3",
            "http",
            Global.StreamNetwork.QUIC
        };

        protected abstract Adapter Adapter { get; }
        protected abstract V2Ray.Outbound.Settings OutboundSettings { get; }

        protected LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public abstract Status FetchDataFromLink(string link);

        public string GetValidRemark() => FileUtility.GetValidFileName(Adapter.remark);

        public Status ValidateStream()
        {
            Adapter adapter = Adapter;

            if (IsRemovedNetwork() || IsObfuscatedKcp() || IsLegacyXtls())
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_TRANSPORT,
                    content: LocalizationService.GetTerm(Localization.UNSUPPORTED_TRANSPORT)
                );

            if (IsInsecureWithoutPinning())
                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: LocalizationService.GetTerm(Localization.CERTIFICATE_CHECK_KEPT)
                );

            return new Status(Code.SUCCESS, SubCode.SUCCESS, null);

            bool IsRemovedNetwork() => removedNetworks.Contains(adapter.streamNetwork);

            bool IsObfuscatedKcp()
            {
                return adapter.streamNetwork == Global.StreamNetwork.KCP
                    && ((!string.IsNullOrEmpty(adapter.headerType) && adapter.headerType != "none")
                    || !string.IsNullOrEmpty(adapter.path));
            }

            bool IsLegacyXtls() => adapter.streamSecurity == Global.StreamSecurity.XTLS;

            bool IsInsecureWithoutPinning()
            {
                return adapter.allowInsecure
                    && string.IsNullOrEmpty(adapter.pinnedPeerCertSha256)
                    && string.IsNullOrEmpty(adapter.verifyPeerCertByName);
            }
        }

        public V2Ray ConvertToV2Ray()
        {
            v2Ray = new V2Ray() {
                log = Log,
                inbounds = Inbounds,
                outbounds = Outbounds
            };

            return v2Ray;
        }

        private V2Ray.Log Log => new V2Ray.Log() {
            loglevel = Global.DEFAULT_LOG_LEVEL,
            access = "",
            error = ""
        };

        private V2Ray.Inbound[] Inbounds => new V2Ray.Inbound[] {
            new V2Ray.Inbound() {
                port = 10801,
                listen = "127.0.0.1",
                protocol = "http",
                settings = new V2Ray.Inbound.Settings() {
                    udp = true
                }
            }
        };

        private V2Ray.Outbound[] Outbounds => new V2Ray.Outbound[] {
            new V2Ray.Outbound() {
                protocol = Adapter.type,
                settings = OutboundSettings,
                streamSettings = new V2Ray.StreamSettings() {
                    network = Adapter.streamNetwork,
                    security = Adapter.streamSecurity,
                    tlsSettings = TlsSettings,
                    wsSettings = WsSettings,
                    grpcSettings = GrpcSettings,
                    tcpSettings = TcpSettings,
                    realitySettings = RealitySettings,
                    xhttpSettings = XhttpSettings,
                    hysteriaSettings = HysteriaSettings,
                    finalmask = FinalMask
                }
            }
        };

        private V2Ray.StreamSettings.TlsSettings TlsSettings
        {
            get
            {
                V2Ray.StreamSettings.TlsSettings tlsSettings = null;

                if (Adapter.streamSecurity == Global.StreamSecurity.TLS)
                {
                    tlsSettings = new V2Ray.StreamSettings.TlsSettings() {
                        fingerprint = Adapter.fingerprint
                    };

                    if (!string.IsNullOrWhiteSpace(Adapter.pinnedPeerCertSha256))
                        tlsSettings.pinnedPeerCertSha256 = Adapter.pinnedPeerCertSha256;

                    if (!string.IsNullOrWhiteSpace(Adapter.verifyPeerCertByName))
                        tlsSettings.verifyPeerCertByName = Adapter.verifyPeerCertByName;

                    if (!string.IsNullOrWhiteSpace(Adapter.echConfigList))
                        tlsSettings.echConfigList = Adapter.echConfigList;

                    if (!string.IsNullOrWhiteSpace(Adapter.alpn))
                        tlsSettings.alpn = new[] { Adapter.alpn };
                    else
                        tlsSettings.alpn = null;

                    if (!string.IsNullOrWhiteSpace(Adapter.sni))
                        tlsSettings.serverName = Adapter.sni;
                    else if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        tlsSettings.serverName = Adapter.requestHost;
                }

                return tlsSettings;
            }
        }

        private V2Ray.StreamSettings.WsSettings WsSettings
        {
            get
            {
                V2Ray.StreamSettings.WsSettings wsSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.WS)
                {
                    wsSettings = new V2Ray.StreamSettings.WsSettings();

                    if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        wsSettings.headers = new V2Ray.StreamSettings.WsSettings.Headers() {
                            Host = Adapter.requestHost
                        };
                    
                    if (!string.IsNullOrWhiteSpace(Adapter.path))
                        wsSettings.path = Adapter.path;
                }

                return wsSettings;
            }
        }

        private V2Ray.StreamSettings.GrpcSettings GrpcSettings
        {
            get
            {
                V2Ray.StreamSettings.GrpcSettings grpcSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.GRPC)
                {
                    grpcSettings = new V2Ray.StreamSettings.GrpcSettings() {
                        serviceName = Adapter.path,
                        multiMode = (Adapter.headerType == "multi")
                    };
                }

                return grpcSettings;
            }
        }

        private V2Ray.StreamSettings.TcpSettings TcpSettings
        {
            get
            {
                V2Ray.StreamSettings.TcpSettings tcpSettings = null;

                if (Adapter.headerType == "http")
                {
                    tcpSettings = new V2Ray.StreamSettings.TcpSettings() {
                        header = new V2Ray.Header() {
                            type = Adapter.headerType,
                            request = GetRequest()
                        }
                    };
                }

                return tcpSettings;

                object GetRequest()
                {
                    string request = @"
                        {'version':'1.1',
                        'method':'GET',
                        'path':[$requestPath$],
                        'headers':{'Host':[$requestHost$],
                        'User-Agent':'',
                        'Accept-Encoding':['gzip, deflate'],
                        'Connection':['keep-alive'],
                        'Pragma':'no-cache'}}
                    ";
                    
                    string[] hostArray = Adapter.requestHost.Split(',');
                    string hostsString = string.Join("','", hostArray);
                    request = request.Replace("$requestHost$", $"'{hostsString}'");

                    string httpPath = "/";
                    if (!string.IsNullOrEmpty(Adapter.path))
                    {
                        string[] pathArray = Adapter.path.Split(',');
                        httpPath = string.Join("','", pathArray);
                    }

                    request = request.Replace("$requestPath$", $"\"{httpPath}\"");
                    return JsonUtility.ConvertFromJson<object>(request);
                }
            }
        }

        private V2Ray.StreamSettings.RealitySettings RealitySettings
        {
            get
            {
                V2Ray.StreamSettings.RealitySettings realitySettings = null;

                if (Adapter.streamSecurity == "reality")
                {
                    realitySettings = new V2Ray.StreamSettings.RealitySettings()
                    {
                        fingerprint = Adapter.fingerprint,
                        serverName = Adapter.sni,
                        publicKey = Adapter.publicKey,
                        shortId = Adapter.shortId,
                        spiderX = Adapter.spiderX
                    };

                    if (!string.IsNullOrWhiteSpace(Adapter.mldsa65Verify))
                        realitySettings.mldsa65Verify = Adapter.mldsa65Verify;
                }

                return realitySettings;
            }
        }

        private V2Ray.StreamSettings.XhttpSettings XhttpSettings
        {
            get
            {
                V2Ray.StreamSettings.XhttpSettings xhttpSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.XHTTP)
                {
                    xhttpSettings = new V2Ray.StreamSettings.XhttpSettings() {
                        path = Adapter.path,
                        mode = Adapter.mode
                    };

                    if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        xhttpSettings.host = Adapter.requestHost;

                    if (!string.IsNullOrWhiteSpace(Adapter.extra))
                        xhttpSettings.extra = JsonUtility.ConvertFromJson<JObject>(Adapter.extra);
                }

                return xhttpSettings;
            }
        }

        private V2Ray.StreamSettings.HysteriaSettings HysteriaSettings
        {
            get
            {
                V2Ray.StreamSettings.HysteriaSettings hysteriaSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.HYSTERIA)
                {
                    hysteriaSettings = new V2Ray.StreamSettings.HysteriaSettings() {
                        version = Adapter.version,
                        auth = Adapter.id
                    };
                }

                return hysteriaSettings;
            }
        }

        private V2Ray.StreamSettings.FinalMask FinalMask
        {
            get
            {
                V2Ray.StreamSettings.FinalMask finalMask = null;

                if (Adapter.obfs == "salamander")
                {
                    finalMask = new V2Ray.StreamSettings.FinalMask() {
                        udp = new V2Ray.StreamSettings.FinalMask.Mask[] {
                            new V2Ray.StreamSettings.FinalMask.Mask() {
                                type = Adapter.obfs,
                                settings = new V2Ray.StreamSettings.FinalMask.Mask.MaskSettings() {
                                    password = Adapter.obfsPassword
                                }
                            }
                        }
                    };
                }

                return finalMask;
            }
        }
    }
}
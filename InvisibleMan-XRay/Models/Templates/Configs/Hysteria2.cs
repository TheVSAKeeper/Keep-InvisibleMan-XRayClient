using System;
using System.Web;
using System.Collections.Specialized;

namespace InvisibleManXRay.Models.Templates.Configs
{
    using Values;

    public class Hysteria2 : Template
    {
        public class Data
        {
            public Uri uri;
            public NameValueCollection query;

            public Data(string url)
            {
                this.uri = new Uri(url);
                this.query = HttpUtility.ParseQueryString(uri.Query);
            }
        }

        private const int DEFAULT_PORT = 443;
        private const string SALAMANDER = "salamander";

        private Data data;

        public override Status FetchDataFromLink(string link)
        {
            try
            {
                // TODO: порт-хоппинг (host:443,5000-6000) Uri не разбирает, и ссылка отклоняется; при первой такой ссылке – разбор портов в finalmask/udphop
                data = new Data(link);
            }
            catch (UriFormatException)
            {
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.INVALID_CONFIG,
                    content: LocalizationService.GetTerm(Localization.INVALID_CONFIG)
                );
            }

            if (IsUnsupportedObfs())
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_LINK,
                    content: LocalizationService.GetTerm(Localization.UNSUPPORTED_CONFIG_LINK)
                );

            return new Status(Code.SUCCESS, SubCode.SUCCESS, null);

            bool IsUnsupportedObfs()
            {
                string obfs = data.query["obfs"];
                return !string.IsNullOrEmpty(obfs) && obfs != SALAMANDER;
            }
        }

        protected override Adapter Adapter => new Adapter() {
            type = "hysteria",
            version = 2,
            remark = data.uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            address = data.uri.IdnHost,
            port = data.uri.Port > 0 ? data.uri.Port : DEFAULT_PORT,
            id = Uri.UnescapeDataString(data.uri.UserInfo),
            streamNetwork = Global.StreamNetwork.HYSTERIA,
            streamSecurity = Global.StreamSecurity.TLS,
            sni = data.query["sni"] ?? "",
            alpn = data.query["alpn"] ?? "",
            allowInsecure = data.query["insecure"] == "1",
            pinnedPeerCertSha256 = data.query["pinSHA256"] ?? data.query["pcs"] ?? "",
            verifyPeerCertByName = data.query["vcn"] ?? "",
            echConfigList = data.query["ech"] ?? "",
            fingerprint = data.query["fp"] ?? "",
            obfs = data.query["obfs"] ?? "",
            obfsPassword = data.query["obfs-password"] ?? ""
        };

        protected override V2Ray.Outbound.Settings OutboundSettings => new V2Ray.Outbound.Settings() {
            version = Adapter.version,
            address = Adapter.address,
            port = Adapter.port
        };
    }
}

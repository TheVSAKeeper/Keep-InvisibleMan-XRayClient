using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers.Templates
{
    using Services;
    using Models;
    using Models.Templates.Subscriptions;
    using Values;

    public class SubscriptionTemplate : ITemplate
    {
        private List<Type> templates;
        private Func<string, Status> convertConfigLinkToV2Ray;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public SubscriptionTemplate()
        {
            this.templates = new List<Type>();
        }

        public void Setup(Func<string, Status> convertConfigLinkToV2Ray)
        {
            this.convertConfigLinkToV2Ray = convertConfigLinkToV2Ray;
        }

        public void RegisterTemplates()
        {
            templates.Add(typeof(Jwt));
            templates.Add(typeof(Simple));
        }

        public Status ConvertLinkToSubscription(string remark, string link)
        {
            Template template = FindTemplate();
            if (template == null)
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_LINK,
                    content: LocalizationService.GetTerm(Localization.UNSUPPORTED_SUBSCRIPTION_LINK)
                );

            Status fetchingStatus = template.FetchDataFromLink(link);
            if (fetchingStatus.Code == Code.ERROR)
                return fetchingStatus;

            bool isAnyTransportUnsupported = false;
            bool isAnyCertificateCheckKept = false;

            List<string[]> v2RayList = template.ConvertToV2RayList(ConvertConfigLink);
            if (Isv2RayListEmpty() && isAnyTransportUnsupported)
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_TRANSPORT,
                    content: LocalizationService.GetTerm(Localization.SUBSCRIPTION_UNSUPPORTED_TRANSPORT)
                );

            if(Isv2RayListEmpty())
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.INVALID_CONFIG,
                    content: LocalizationService.GetTerm(Localization.INVALID_SUBSCRIPTION)
                );

            return new Status(
                code: Code.SUCCESS,
                subCode: SubCode.SUCCESS,
                content: new string[] {
                    template.GetValidRemark(remark),
                    JsonConvert.SerializeObject(v2RayList),
                    GetWarning()
                }
            );

            Status ConvertConfigLink(string configLink)
            {
                Status convertingStatus = convertConfigLinkToV2Ray.Invoke(configLink);

                if (convertingStatus.SubCode == SubCode.UNSUPPORTED_TRANSPORT)
                    isAnyTransportUnsupported = true;
                else if (convertingStatus.Code == Code.SUCCESS && HasWarning(convertingStatus))
                    isAnyCertificateCheckKept = true;

                return convertingStatus;

                bool HasWarning(Status configStatus)
                {
                    string[] config = (string[])configStatus.Content;
                    return config.Length > 2 && !string.IsNullOrEmpty(config[2]);
                }
            }

            string GetWarning()
            {
                List<string> warnings = new List<string>();

                if (isAnyTransportUnsupported)
                    warnings.Add(LocalizationService.GetTerm(Localization.SUBSCRIPTION_UNSUPPORTED_TRANSPORT));

                if (isAnyCertificateCheckKept)
                    warnings.Add(LocalizationService.GetTerm(Localization.SUBSCRIPTION_CERTIFICATE_CHECK_KEPT));

                return warnings.Count > 0 ? string.Join("\n\n", warnings) : null;
            }

            Template FindTemplate()
            {
                foreach(Type type in templates)
                {
                    Template template = Activator.CreateInstance(type) as Template;
                    if (template.IsValid(link))
                        return template;
                }

                return null;
            }

            bool Isv2RayListEmpty() => v2RayList.Count == 0;
        }
    }
}
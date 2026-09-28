namespace InvisibleManXRay.Models
{
    public enum Code { SUCCESS, ERROR, INFO }
    public enum SubCode { 
        NO_CONFIG = 0, INVALID_CONFIG = 1, SUCCESS = 2, UNSUPPORTED_LINK = 3, CANT_CONNECT = 4,
        UPDATE_AVAILABLE = 5, UPDATE_UNAVAILABLE = 6, BROADCAST_UNAVAILABLE = 7, CANT_CONNECT_TO_TUNNEL_SERVICE = 8,
        CANT_PROXY = 9, CANT_TUNNEL = 10, CANCELED = 11, UNSUPPORTED_TRANSPORT = 12
    }

    public class Status
    {
        private Code code;
        private SubCode subCode;
        private object content;

        public Code Code => code;
        public SubCode SubCode => subCode;
        public object Content => content;

        public Status(Code code, SubCode subCode, object content)
        {
            this.code = code;
            this.subCode = subCode;
            this.content = content;
        }
    }
}
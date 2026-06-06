using System;

namespace OnceMi.AspNetCore.OSS.Error
{
    public class OSSException : Exception
    {
        public OSSException(OSSError error) : base(error.Message)
        {
            ErrorCode = error.Code;
        }

        public OSSException(OSSError error, Exception ex) : base(error.Message, ex)
        {
            ErrorCode = error.Code;
            ProviderMessage = ex?.Message;
        }

        public int ErrorCode { get; private set; }

        public string ProviderMessage { get; set; }
    }
}
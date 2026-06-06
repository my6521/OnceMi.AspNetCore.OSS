using OnceMi.AspNetCore.OSS.Utils;

namespace OnceMi.AspNetCore.OSS.Error
{
    public static class OSSErrorExtensions
    {
        public static OSSError ToOSSError(this OSSErrorCode code) => new OSSError()
        {
            Code = (int)code,
            Message = code.GetDisplayContent()
        };
    }
}
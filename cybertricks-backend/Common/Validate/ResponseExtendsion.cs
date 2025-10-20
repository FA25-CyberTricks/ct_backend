using ct.backend.Common.Constants;
using ct.backend.Features;

namespace ct.backend.Common.Validate
{
    public static class ResponseExtensions
    {
        public static void AddError<T>(
            this AbstractResponse<T> response,
            string message,
            string field = "General")
        {
              var code = nameof(message);     

            response.Errors.Add(new ErrorDetail
            {
                Code = code,
                Message = message,
                Field = field
            });

            if (code != MessageCodes.E000)
                response.Success = false;
        }

        public static void AddError<T>(
            this AbstractResponse<T> response,
            string code,
            string description,
            string field = "General")
        {
            response.Errors.Add(new ErrorDetail
            {
                Code = code,
                Message = description,
                Field = field
            });

            if (code != "E000")
                response.Success = false;
        }
    }
}


using System.Globalization;
using System.Net;

namespace ApiApp.Exceptions;

public class ClientResponseException : AppException
{
    public ClientResponseException(HttpStatusCode clientResponseCode) : base() {
        StatusCode = clientResponseCode;
    }

    public ClientResponseException(string message, HttpStatusCode clientResponseCode) : base(message)
    {
        StatusCode = clientResponseCode;
    }

    public ClientResponseException(string message, HttpStatusCode clientResponseCode, params object[] args)
        : base(string.Format(CultureInfo.CurrentCulture, message, args))
    {
        StatusCode = clientResponseCode;
    }

    public HttpStatusCode StatusCode { get; set; }
}

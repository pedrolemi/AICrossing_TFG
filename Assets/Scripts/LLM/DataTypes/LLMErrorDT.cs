using System;

namespace LLM
{
    // Clase que indica el estado de un mensaje a red
    // Se usa para propagar el estado de dicho mensaje y poder tratarlo
    // cuando se produce un error, como si fuera una promesa
    public class Result<T>
    {
        public T Value { get; private set; }
        public string Error { get; private set; }
        public bool IsSucess => Error == null;
        private Result(T value, string error)
        {
            Value = value;
            Error = error;
        }
        public static Result<T> Success(T value) => new Result<T>(value, null);
        public static Result<T> Fail(string error) => new Result<T>(default, error);
    }

    [Serializable]
    public class Error
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
    }

    [Serializable]
    public class ApiError
    {
        public Error Error { get; set; }
        public string GetErrorInfo()
        {
            return $"API request failed with error type {Error.Type.Trim()} and error code {Error.Code.Trim()}. " +
                $"Error message: {Error.Message.Trim()}.";
        }
    }
}
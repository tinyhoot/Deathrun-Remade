using System;

namespace DeathrunRemade.Objects.Exceptions
{
    public class DeathrunException : Exception
    {
        public DeathrunException(){}
        public DeathrunException(string message) : base(message){}
    }
}
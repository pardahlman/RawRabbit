﻿using System;

namespace RawRabbit.Exceptions
{
    public class PublishConfirmException : Exception
    {
        public PublishConfirmException()
        { }

        public PublishConfirmException(string message) :base(message)
        { }

        public PublishConfirmException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}

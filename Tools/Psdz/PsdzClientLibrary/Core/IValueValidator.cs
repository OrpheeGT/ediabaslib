﻿namespace PsdzClient.Core
{
    public interface IValueValidator
    {
        bool IsValid<T>(string propertyName, object value);
    }
}

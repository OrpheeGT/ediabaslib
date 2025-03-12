﻿using System.ComponentModel;
using System;

namespace PsdzClient.Core
{
    public interface IInteractionModel : INotifyPropertyChanged
    {
        Guid Guid { get; }

        string Title { get; }

        bool IsCloseButtonEnabled { get; }

        bool IsPrintButtonVisible { get; }

        int DialogSize { get; }

        event EventHandler ModelClosedByUser;
    }
}

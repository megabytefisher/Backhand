namespace Backhand.Protocols.Dlp
{
    public enum DlpErrorCode : ushort
    {
        Success                     = 0x00,
        SystemError                 = 0x01,
        IllegalRequestError         = 0x02,
        OutOfMemoryError            = 0x03,
        InvalidArgError             = 0x04,
        NotFoundError               = 0x05,
        NoneOpenError               = 0x06,
        AlreadyOpenError            = 0x07,
        TooManyOpenError            = 0x08,
        AlreadyExistsError          = 0x09,
        OpenError                   = 0x0a,
        DeletedError                = 0x0b,
        BusyError                   = 0x0c,
        UnsupportedError            = 0x0d,
        //Unused
        ReadOnlyError               = 0x0f,
        SpaceError                  = 0x10,
        LimitError                  = 0x11,
        UserCancelledError          = 0x12,
        InvalidArgWrapperError      = 0x13,
        MissingArgError             = 0x14,
        InvalidArgSize              = 0x15,
        UnknownError                = 0x7f,
    }
}

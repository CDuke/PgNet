namespace PgNet.BackendMessage
{
    internal enum AuthenticationRequestType : int
    {
        Ok = 0,
        KerberosV5 = 2,
        CleartextPassword = 3,
        MD5Password = 5,
        SCMCredential = 6,
        GSS = 7,
        GSSContinue = 8,
        SSPI = 9,
        SASL = 10,
        SASLContinue = 11,
        SASLFinal = 12
    }
}

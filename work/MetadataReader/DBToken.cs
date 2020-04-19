namespace OneCSharp.SQL
{
    internal static class DBToken
    {
        internal const string L = "L"; // boolean
        internal const string N = "N"; // numeric
        internal const string S = "S"; // string
        internal const string D = "D"; // Config metadata (SDBL) - date
        internal const string T = "T"; // SQL tables fields (RDBMS) - date
        internal const string B = "B"; // binary
        internal const string RRef = "RRef";
        internal const string TRef = "TRef";
        internal const string RRRef = "RRRef";
        internal const string RTRef = "RTRef";
        internal const string TYPE = "TYPE";

        internal const string Fld = "Fld";
        internal const string IDRRef = "IDRRef";
        internal const string Version = "Version";
        internal const string Marked = "Marked";
        internal const string DateTime = "DateTime";
        internal const string NumberPrefix = "NumberPrefix";
        internal const string Number = "Number";
        internal const string Posted = "Posted";
        internal const string PredefinedID = "PredefinedID";
        internal const string Description = "Description";
        internal const string Code = "Code";
        internal const string OwnerID = "OwnerID";
        internal const string Folder = "Folder";
        internal const string ParentIDRRef = "ParentIDRRef";

        internal const string KeyField = "KeyField";
        internal const string LineNo = "LineNo";
        internal const string EnumOrder = "EnumOrder";
        
        internal const string Period = "Period";
        internal const string Recorder = "Recorder";
        internal const string RecorderRRef = "RecorderRRef";
        internal const string RecorderTRef = "RecorderTRef";
        internal const string Active = "Active";
        internal const string RecordKind = "RecordKind";

        internal const string VT = "VT";
        internal const string Enum = "Enum";
        internal const string Node = "Node";
        internal const string Const = "Const";
        internal const string Reference = "Reference";
        internal const string Document = "Document";
        internal const string InfoRg = "InfoRg";
        internal const string AccumRg = "AccumRg";
        internal const string ChngR = "ChngR";
    }
}

// IDRRef Version Marked Date_Time NumberPrefix Number Posted Document503_IDRRef KeyField LineNo
// PredefinedID Description Code OwnerID Folder Reference38_IDRRef ParentIDRRef
// [_OwnerIDRRef] | [_OwnerID_TYPE] + [_OwnerID_RTRef] + [_OwnerID_RRRef]
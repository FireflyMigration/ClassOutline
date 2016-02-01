// Guids.cs
// MUST match guids.h
using System;

namespace ClassOutline
{
    static class GuidList
    {
        public const string guidVSPackage1PkgString = "08239c20-ccf6-4f5f-8c36-0b5669cb462e";
        public const string guidVSPackage1CmdSetString = "6379af62-42e5-4662-a6ff-5483e906c8a7";
        public const string guidToolWindowPersistanceString = "16f05c88-11ee-408d-ae8c-d6b790441652";

        public static readonly Guid guidVSPackage1CmdSet = new Guid(guidVSPackage1CmdSetString);
    };
}
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections;
using System.Text;
using System.Reflection;
using Microsoft.VsSDK.UnitTestLibrary;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using ClassOutline;

namespace VSPackage1_UnitTests.ClassOutlineToolWindowTest
{
    /// <summary>
    ///This is a test class for ClassOutlineToolWindowTest and is intended
    ///to contain all ClassOutlineToolWindowTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ClassOutlineToolWindowTest
    {

        /// <summary>
        ///ClassOutlineToolWindow Constructor test
        ///</summary>
        [TestMethod()]
        public void ClassOutlineToolWindowConstructorTest()
        {

            ClassOutlineToolWindow target = new ClassOutlineToolWindow();
            Assert.IsNotNull(target, "Failed to create an instance of ClassOutlineToolWindow");

            MethodInfo method = target.GetType().GetMethod("get_Content", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method.Invoke(target, null), "ClassOutlineControl object was not instantiated");

        }

        /// <summary>
        ///Verify the Content property is valid.
        ///</summary>
        [TestMethod()]
        public void WindowPropertyTest()
        {
            ClassOutlineToolWindow target = new ClassOutlineToolWindow();
            Assert.IsNotNull(target.Content, "Content property was null");
        }

    }
}

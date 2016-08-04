﻿//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.HttpTests
{
    [TestClass]
    public class MsalHttpRequestTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }


        [TestMethod]
        public void TestSendPostNullHeaderNullBody()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            MsalHttpResponse response =
                MsalHttpRequest.SendPost(new Uri(TestConstants.DefaultAuthorityHomeTenant + "oauth2/token"),
                    null, null, null).Result;

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(MockHelpers.DefaultAccessTokenResponse, response.Body);
        }

        [TestMethod]
        public void TestSendPostNoFailure()
        {
            Dictionary<string, string> bodyParameters = new Dictionary<string, string> { ["key1"] = "some value1", ["key2"] = "some value2" };
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                PostData = bodyParameters,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });


            MsalHttpResponse response =
                MsalHttpRequest.SendPost(new Uri(TestConstants.DefaultAuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), bodyParameters, null).Result;

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(MockHelpers.DefaultAccessTokenResponse, response.Body);
        }

        [TestMethod]
        public void TestSendGetNoFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            MsalHttpResponse response =
                MsalHttpRequest.SendPost(new Uri(TestConstants.DefaultAuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), new Dictionary<string, string>(), null).Result;

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(MockHelpers.DefaultAccessTokenResponse, response.Body);
        }

        [TestMethod]
        public void TestSendGetWithRetryOnTimeoutFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            try
            {
                var msalHttpResponse = MsalHttpRequest.SendGet(new Uri(TestConstants.DefaultAuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), null).Result;
                Assert.Fail("request should have failed");
            }
            catch (AggregateException exc)
            {
                Assert.IsNotNull(exc);
                Assert.IsTrue(exc.InnerException is RetryableRequestException);
            }
        }

        [TestMethod]
        public void TestSendPostWithRetryOnTimeoutFailure()
        {
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateRequestTimeoutResponseMessage(),
                ExceptionToThrow = new TaskCanceledException("request timed out")
            });

            try
            {
                var msalHttpResponse = MsalHttpRequest.SendPost(new Uri(TestConstants.DefaultAuthorityHomeTenant + "oauth2/token"),
                    new Dictionary<string, string>(), new Dictionary<string, string>(), null).Result;
                Assert.Fail("request should have failed");
            }
            catch (AggregateException exc)
            {
                Assert.IsNotNull(exc);
                Assert.IsTrue(exc.InnerException is RetryableRequestException);
            }
        }
    }
}

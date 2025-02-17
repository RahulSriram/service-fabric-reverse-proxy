﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using System.Net.Http;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace ReverseProxyService.Utilities
{
  public class HttpCommunicationClient : ICommunicationClient
  {
    public HttpCommunicationClient(HttpClient client, string address)
    {
      this.HttpClient = client;
      this.Url = new Uri(address);
    }

    public HttpClient HttpClient { get; }

    public Uri Url { get; }

    ResolvedServiceEndpoint ICommunicationClient.Endpoint { get; set; }

    string ICommunicationClient.ListenerName { get; set; }

    ResolvedServicePartition ICommunicationClient.ResolvedServicePartition { get; set; }
  }
}
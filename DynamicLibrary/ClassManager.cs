﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    public class ClassManager
    {
        private List<Stream> m_providerStreams;
        private List<TcpClient> m_providerClients;
        private ClassDeserializer m_deserializer;

        public ClassManager()
        {
            m_providerStreams = new List<Stream>();
            m_providerClients = new List<TcpClient>();
            m_deserializer = new ClassDeserializer(this);
        }

        public void AddProvider(Stream provider)
        {
            m_providerStreams.Add(provider);
        }

        public void AddProvider(string hostname, int port)
        {
            try
            {
                var client = new TcpClient(hostname, port);
                if (client.Connected)
                {
                    AddProvider(client.GetStream());
                    m_providerClients.Add(client);
                }
            }
            catch(Exception e)
            {
                if (Debugger.IsAttached)
                    AddProvider(hostname, port);
            }
        }

        public object Instance(string name, params object[] p)
        {
            return m_deserializer.Instance(ResolveType(name), p);
        }

        private Type ResolveType(string name)
        {
            return m_deserializer.ResolveType(name);
        }

        internal Type RequestType(string name)
        {
            var m = new RequestMessage() { FullName = name };
            foreach(var s in m_providerStreams)
            {
                m.SerializeTo(s);
                var resp = s.DeserializeFromAsync<ResponseMessage>();
                resp.Wait();
                if (resp != null && resp.Result.TypeFollowing)
                {
                    m_deserializer.SetStream(s);
                    return m_deserializer.Deserialize();
                }
            }

            return null;
        }
    }
}

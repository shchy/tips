using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tips.Model.Models;

namespace Tips.Model.Context
{
    public class FileDataBaseContext : InMemoryDataBaseContext, IDisposable
    {
        private string sourcePath;

        public FileDataBaseContext(string sourcePath)
        {
            this.sourcePath = sourcePath;
            this.inMemoryData = TryLoad(this.sourcePath);

        }

        private InMemoryData TryLoad(string sourcePath)
        {
            return
                Fn.New(() =>
                {
                    var toXml = new XmlSerializer(typeof(InMemoryData));
                    using (var fs = File.OpenRead(sourcePath))
                    {
                        return (InMemoryData)toXml.Deserialize(fs);
                    }
                }).ToExceptional()
                .Return(()=>new InMemoryData());
        }

        public void Dispose()
        {
            TrySave(this.sourcePath, this.inMemoryData);
        }

        private void TrySave(string sourcePath, InMemoryData inMemoryData)
        {
            using (var fs = new FileStream(sourcePath, FileMode.Create))
            {
                var toXml = new XmlSerializer(typeof(InMemoryData));
                toXml.Serialize(fs, inMemoryData);
            }
        }
    }
}

using Microsoft.AspNetCore.Components;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Text;
using W8lessLabs.Blazor.LocalFiles;

namespace protogen.site.Pages
{
    public class DecodeModel : ComponentBase
    {
        public byte[] Data { get; set; }
        public bool Deep { get; set; } = true;
        public string HexData { get; set; }
        public string Base64Data { get; set; }
        public FileSelect FileSelect { get; set; }

        public void DecodeHex()
        {
            string hex = HexData?.Trim();
            if (!string.IsNullOrWhiteSpace(hex))
            {
                hex = hex.Replace(" ", "").Replace("-", "").Replace("\r", "").Replace("\n", "");
                int len = hex.Length / 2;
                var tmp = new byte[len];
                for (int i = 0; i < len; i++)
                {
                    tmp[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                Data = tmp;
            }
        }

        public void DecodeBase64()
        {
            string base64 = Base64Data?.Trim();
            if (!string.IsNullOrWhiteSpace(base64))
            {
                Data = Convert.FromBase64String(base64);
            }
        }

        public void DecodeFile()
        {
            FileSelect.SelectFiles(async (selectedFiles) =>
            {
                using (var fileReader = FileSelect.GetFileReader(selectedFiles.First()))
                {
                    Data = await fileReader.GetFileBytesAsync();
                    StateHasChanged();
                }
            });
        }
    }

    public class DecodeBytesModel
    {
        private ArraySegment<byte> data;
        public bool Deep { get; }

        public int SkipField { get; }

        private DecodeBytesModel(byte[] data, bool deep, int offset, int count, int skipField = 0)
        {
            this.data = data == null
                ? default
                : new ArraySegment<byte>(data, offset, count);
            Deep = deep;
            SkipField = skipField;
        }
        public DecodeBytesModel(byte[] data, bool deep) : this(data, deep, 0, data?.Length ?? 0) { }

        public string AsHex() => ContainsValue ? BitConverter.ToString(data.Array, data.Offset, data.Count) : null;

        public string AsHex(long offset, long count) => ContainsValue ? BitConverter.ToString(data.Array, (int)(data.Offset + offset), (int)count) : null;
        public string AsBase64() => ContainsValue ? Convert.ToBase64String(data.Array, data.Offset, data.Count) : null;
        public string AsString()
        {
            try
            {
                return Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
            }
            catch { return null; }
        }
        public int Count => data.Count;
        public ProtoReader GetReader(out ProtoReader.State state)
        {
            var ms = new MemoryStream(data.Array, data.Offset, data.Count, false);
            return ProtoReader.Create(out state, ms, null, null);
        }
        public bool ContainsValue => data.Array != null;
        public bool CouldBeProto()
        {
            if (!ContainsValue) return false;
            try
            {
                using (var reader = GetReader(out var state))
                {
                    int field;
                    while ((field = reader.ReadFieldHeader(ref state)) > 0)
                    {
                        reader.SkipField(ref state);
                    }
                    return reader.GetPosition(ref state) == Count; // MemoryStream will let you seek out of bounds!
                }
            }
            catch
            {
                return false;
            }
        }
        public DecodeBytesModel Slice(long offset, long count, long skipField = 0) => new DecodeBytesModel(data.Array, Deep, (int)(data.Offset + offset), (int)count, (int)skipField);
    }
}
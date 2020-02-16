using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;

namespace LibDmd.Output.FileOutput
{
    // Hijacking bitmap output for my needs
    // TODO: if this ever goes up stream we should make a different output or add a bunch of config
    // vars to maintain the other bitmap behavior
	public class WdaOutput : IRgb24Destination, IResizableDestination {
		public string OutputFolder { get; set; }

		public string Name { get; } = "File Writer";
		public bool IsAvailable { get; } = true;

        private int _width = 128;
        private int _height = 32;
		private string _prevHash;
        private FileStream _wdaFile;
        private BinaryWriter _wdaWriter;
        private Stopwatch _stopwatch = new Stopwatch();
        private readonly SHA1CryptoServiceProvider _hashProvider = new SHA1CryptoServiceProvider();
        private bool _wroteHeader = false;

		public WdaOutput(string outputFolder, string gameName)
		{
			OutputFolder = outputFolder;
            string fileName = $"{gameName}.{(uint)DateTime.Now.TimeOfDay.TotalSeconds}.{DateTime.Now.DayOfYear}.{DateTime.Now.Year}.wda";
            _wdaFile = File.Create( Path.Combine( OutputFolder, fileName ) );
            _wdaWriter = new BinaryWriter( _wdaFile );
            _stopwatch.Start();
		}

		public void Init()
		{
		}

        public void SetDimensions( int width, int height ) {
            if( _wroteHeader && (width != _width || height != _height) ) {
                CloseFile();
                return;
            }
            _width = width;
            _height = height;

            if( !_wroteHeader ) {
                WriteHeader();
            }
        }
        
        public void RenderRgb24( byte[] frame ) {
            if( _wdaWriter == null ) return;

            var hash = Convert.ToBase64String( _hashProvider.ComputeHash( frame ) );
            if( _prevHash == hash ) {
                return;
            }
            _prevHash = hash;

            if( !_wroteHeader ) {
                WriteHeader();
            }

            _wdaWriter.Write( (uint)_stopwatch.ElapsedMilliseconds );
            _wdaWriter.Write( (uint)0 );
            _wdaWriter.Write( (byte)0 );
            _wdaWriter.Write( (byte)0 );
            _wdaWriter.Write( (byte)0 );
            _wdaWriter.Write( frame, 0, _width * _height * 3 );
        }

        public void SetColor( Color color ) {
            // ignore
        }

        public void SetPalette( Color[] colors, int index = -1 ) {
            // ignore
        }

        public void ClearPalette() {
            // ignore
        }

        public void ClearColor() {
            // ignore
        }

        public void ClearDisplay() {
            // ignore
        }

        public void Dispose()
		{
            _hashProvider.Dispose();
            CloseFile();

        }

        private void CloseFile() {
            _wdaFile?.Dispose();
            _wdaFile = null;

            _wdaWriter?.Dispose();
            _wdaWriter = null;
        }

        private void WriteHeader() {
            _wroteHeader = true;

            _wdaWriter.Write( new char[] { 'W', 'D', 'A', '.' } );
            _wdaWriter.Write( (ushort)_width );
            _wdaWriter.Write( (ushort)_height );
            _wdaWriter.Write( (uint)12 );
        }
	}
}

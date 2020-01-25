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
	public class BitmapOutput : IRgb24Destination, IResizableDestination {
		public string OutputFolder { get; set; }

		public string Name { get; } = "File Writer";
		public bool IsAvailable { get; } = true;

		private int _counter;
        private int _width = 128;
        private int _height = 32;
		private string _prevHash;
        private TextWriter _manifestWriter;
        private Stopwatch _stopwatch = new Stopwatch();
        private readonly SHA1CryptoServiceProvider _hashProvider = new SHA1CryptoServiceProvider();

		public BitmapOutput(string outputFolder)
		{
			OutputFolder = outputFolder;
            _manifestWriter = File.CreateText( OutputFolder + @"\manifest.txt" );
            _stopwatch.Start();
		}

		public void Init()
		{
		}

        public void SetDimensions( int width, int height ) {
            _width = width;
            _height = height;
        }
        
        public void RenderRgb24( byte[] frame ) {
            var hash = Convert.ToBase64String( _hashProvider.ComputeHash( frame ) );
            if( _prevHash == hash ) {
                return;
            }
            _prevHash = hash;

            var fileName = @"frame_" + _counter.ToString( "D5" ) + ".bmp";
            _manifestWriter.WriteLine( $"{fileName},{_stopwatch.ElapsedMilliseconds}" );

            var filePath = Path.Combine( OutputFolder, fileName );
            using( var fileStream = new FileStream( filePath, FileMode.Create ) ) {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                int stride = (_width * PixelFormats.Rgb24.BitsPerPixel + 7) / 8;
                BitmapSource bitmap = BitmapSource.Create( _width, _height, 96, 96, PixelFormats.Rgb24, null, frame, stride );
                encoder.Frames.Add( BitmapFrame.Create( bitmap ) );
                encoder.Save( fileStream );
            }
            _counter++;
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
            _manifestWriter.Dispose();

        }
	}

    public class InvalidFolderException : Exception {
        public InvalidFolderException( string message ) : base( message ) {
        }
    }
}

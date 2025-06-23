using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CryptoGuard.Core.Interfaces;
using NAudio.Wave;

namespace CryptoGuard.Infrastructure.Services
{
    public class VoiceRecognitionService : IVoiceRecognitionService
    {
        private readonly string _deepgramApiKey = "cae8c97090fc7515788a23443318df011b81a069";
        private WaveInEvent? _waveIn;
        private MemoryStream? _audioStream;
        private bool _isListening;
        private TaskCompletionSource<string>? _recognitionTaskSource;

        public bool IsListening => _isListening;

        public async Task<string> RecognizeSpeechAsync()
        {
            _recognitionTaskSource = new TaskCompletionSource<string>();
            await StartListeningAsync();
            var result = await _recognitionTaskSource.Task;
            await StopListeningAsync();
            return result;
        }

        public Task<bool> StartListeningAsync()
        {
            if (_isListening) return Task.FromResult(true);
            _audioStream = new MemoryStream();
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1)
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.StartRecording();
            _isListening = true;
            return Task.FromResult(true);
        }

        public Task StopListeningAsync()
        {
            if (!_isListening) return Task.CompletedTask;
            _waveIn?.StopRecording();
            _isListening = false;
            return Task.CompletedTask;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _audioStream?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private async void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (_audioStream == null)
            {
                _recognitionTaskSource?.TrySetException(new Exception("Audio stream is null"));
                return;
            }
            var audioBytes = _audioStream.ToArray();
            var text = await SendToDeepgramAsync(audioBytes);
            _recognitionTaskSource?.TrySetResult(text);
            _audioStream.Dispose();
            _audioStream = null;
        }

        private async Task<string> SendToDeepgramAsync(byte[] audioBytes)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _deepgramApiKey);
            using var content = new ByteArrayContent(audioBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            var response = await client.PostAsync("https://api.deepgram.com/v1/listen", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            // Basit JSON parse (Deepgram response: { "results": { "channels": [ { "alternatives": [ { "transcript": "..." } ] } ] } })
            var transcript = ExtractTranscript(json);
            return transcript;
        }

        private string ExtractTranscript(string json)
        {
            // Çok basit bir parse, prod'da JSON parser kullan
            var marker = "\"transcript\":\"";
            var start = json.IndexOf(marker);
            if (start < 0) return string.Empty;
            start += marker.Length;
            var end = json.IndexOf('"', start);
            if (end < 0) return string.Empty;
            return json.Substring(start, end - start);
        }
    }
} 
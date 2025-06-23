using System;
using System.Threading.Tasks;
using Vosk;
using NAudio.Wave;
using System.IO;

namespace CryptoGuard.MAUI.Services
{
    public class SpeechService : IDisposable
    {
        private readonly string _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "vosk-model-small-tr-0.3");
        private Model? _model;
        private WaveInEvent? _waveIn;
        private VoskRecognizer? _recognizer;
        private bool _isListening;
        public event Action<string>? OnCommandRecognized;
        public event Action? OnWakeWordDetected;

        public SpeechService()
        {
            Vosk.Vosk.SetLogLevel(0);
            if (!Directory.Exists(_modelPath))
                throw new DirectoryNotFoundException($"Vosk model path not found: {_modelPath}");
            _model = new Model(_modelPath);
        }

        private void CleanupAudio()
        {
            try { _waveIn?.StopRecording(); } catch { }
            try { _waveIn?.Dispose(); } catch { }
            _waveIn = null;
            try { _recognizer?.Dispose(); } catch { }
            _recognizer = null;
        }

        public void StartWakeWordListening()
        {
            if (_isListening) return;
            CleanupAudio();
            _isListening = true;
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
            _recognizer = new VoskRecognizer(_model, 16000.0f);
            _waveIn.DataAvailable += WakeWordDataAvailable;
            _waveIn.StartRecording();
        }

        private void WakeWordDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_recognizer == null) return;
            try
            {
                if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    var result = _recognizer.Result();
                    if (result.Contains("asistan"))
                    {
                        CleanupAudio();
                        _isListening = false;
                        OnWakeWordDetected?.Invoke();
                    }
                }
            }
            catch
            {
                CleanupAudio();
                _isListening = false;
            }
        }

        public void StartCommandListening()
        {
            if (_isListening) return;
            CleanupAudio();
            _isListening = true;
            _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
            _recognizer = new VoskRecognizer(_model, 16000.0f);
            _waveIn.DataAvailable += CommandDataAvailable;
            _waveIn.StartRecording();
        }

        private void CommandDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_recognizer == null) return;
            try
            {
                if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    var result = _recognizer.Result();
                    var text = ExtractText(result);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        CleanupAudio();
                        _isListening = false;
                        OnCommandRecognized?.Invoke(text);
                    }
                }
            }
            catch
            {
                CleanupAudio();
                _isListening = false;
            }
        }

        private string ExtractText(string voskResult)
        {
            // Vosk JSON sonucu: { "text" : "..." }
            var idx = voskResult.IndexOf(":");
            if (idx < 0) return string.Empty;
            var text = voskResult.Substring(idx + 1).Trim(' ', '"', '}', '\n');
            return text;
        }

        public void Dispose()
        {
            CleanupAudio();
            _model?.Dispose();
        }
    }
} 
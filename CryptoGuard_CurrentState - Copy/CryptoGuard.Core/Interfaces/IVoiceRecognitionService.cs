using System.Threading.Tasks;

namespace CryptoGuard.Core.Interfaces
{
    public interface IVoiceRecognitionService
    {
        Task<string> RecognizeSpeechAsync();
        Task<bool> StartListeningAsync();
        Task StopListeningAsync();
        bool IsListening { get; }
    }
} 
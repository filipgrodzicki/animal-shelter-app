/**
 * Animacja pisania asystenta
 */
export function TypingIndicator() {
  return (
    <div className="flex justify-start">
      <div className="bg-gray-100 text-gray-800 px-4 py-3 rounded-2xl rounded-bl-none max-w-[80%]">
        <div className="flex items-center space-x-1">
          <div
            className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
            style={{ animationDelay: '0ms', animationDuration: '0.6s' }}
          />
          <div
            className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
            style={{ animationDelay: '150ms', animationDuration: '0.6s' }}
          />
          <div
            className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
            style={{ animationDelay: '300ms', animationDuration: '0.6s' }}
          />
        </div>
      </div>
    </div>
  );
}

import { useEffect, useRef, useState, useCallback, KeyboardEvent } from 'react';
import {
  PaperAirplaneIcon,
  ArrowPathIcon,
  XMarkIcon,
} from '@heroicons/react/24/solid';
import { useChatbotStore } from '../../stores/chatbotStore';
import { ChatMessage } from './ChatMessage';
import { TypingIndicator } from './TypingIndicator';

const MIN_W = 340;
const MIN_H = 400;
const MAX_W = 900;
const MAX_H = 900;
const DEFAULT_W = 400;
const DEFAULT_H = 520;

/**
 * Okno czatu z asystentem
 */
export function ChatWindow() {
  const [input, setInput] = useState('');
  const [size, setSize] = useState({ w: DEFAULT_W, h: DEFAULT_H });
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const resizing = useRef(false);
  const resizeStart = useRef({ x: 0, y: 0, w: 0, h: 0 });

  const {
    messages,
    isLoading,
    error,
    sendMessage,
    clearSession,
    clearError,
  } = useChatbotStore();
  const toggle = useChatbotStore((s) => s.toggle);

  // Auto-scroll do najnowszej wiadomosci
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  // Focus na input po otwarciu
  useEffect(() => {
    const timer = setTimeout(() => {
      inputRef.current?.focus();
    }, 100);
    return () => clearTimeout(timer);
  }, []);

  const handleSubmit = async () => {
    if (!input.trim() || isLoading) return;
    const message = input.trim();
    setInput('');
    await sendMessage(message);
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleNewChat = async () => {
    clearSession();
    // Poczekaj na wyczyszczenie stanu przed rozpoczęciem nowej sesji
    await new Promise(resolve => setTimeout(resolve, 100));
    await useChatbotStore.getState().startSession();
  };

  // --- Drag-to-resize z lewego-gornego rogu ---
  const onResizeStart = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      resizing.current = true;
      resizeStart.current = { x: e.clientX, y: e.clientY, w: size.w, h: size.h };

      const onMove = (ev: MouseEvent) => {
        if (!resizing.current) return;
        // ciagniecie w lewo = wieksza szerokosc, w gore = wieksza wysokosc
        const dw = resizeStart.current.x - ev.clientX;
        const dh = resizeStart.current.y - ev.clientY;
        setSize({
          w: Math.min(MAX_W, Math.max(MIN_W, resizeStart.current.w + dw)),
          h: Math.min(MAX_H, Math.max(MIN_H, resizeStart.current.h + dh)),
        });
      };

      const onUp = () => {
        resizing.current = false;
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
      };

      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    },
    [size],
  );

  // --- Style kontenera ---
  const containerStyle: React.CSSProperties = {
    position: 'absolute', bottom: 64, right: 0, width: size.w, height: size.h,
  };

  return (
    <div
      style={containerStyle}
      className="bg-white rounded-2xl shadow-2xl flex flex-col overflow-hidden border border-gray-200"
      role="dialog"
      aria-label="Czat z asystentem schroniska"
    >
      {/* Resize handle – lewy gorny rog */}
      <div
        onMouseDown={onResizeStart}
        className="absolute top-0 left-0 w-4 h-4 cursor-nw-resize z-10"
        title="Przeciagnij aby zmienic rozmiar"
      >
        <svg
          className="w-3 h-3 m-0.5 text-gray-300"
          viewBox="0 0 10 10"
          fill="currentColor"
        >
          <circle cx="1.5" cy="1.5" r="1.2" />
          <circle cx="5" cy="1.5" r="1.2" />
          <circle cx="1.5" cy="5" r="1.2" />
        </svg>
      </div>

      {/* Header */}
      <div className="bg-primary-600 text-white px-5 py-3 flex items-center justify-between shrink-0">
        <div>
          <h2 className="font-semibold text-sm">Asystent Schroniska</h2>
          <p className="text-[11px] text-primary-200">
            Odpowiem na pytania o adopcje i schronisko
          </p>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={handleNewChat}
            className="p-2 hover:bg-white/10 rounded-lg transition-colors"
            title="Nowa rozmowa"
            aria-label="Rozpocznij nowa rozmowe"
          >
            <ArrowPathIcon className="h-4 w-4" />
          </button>
          <button
            onClick={toggle}
            className="p-2 hover:bg-white/10 rounded-lg transition-colors"
            title="Zamknij czat"
            aria-label="Zamknij czat"
          >
            <XMarkIcon className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Messages */}
      <div
        className="flex-1 overflow-y-auto px-4 py-4 space-y-3 bg-gray-50"
        role="log"
        aria-live="polite"
      >
        {messages.length === 0 && !isLoading && (
          <div className="text-center text-gray-400 mt-12 px-6">
            <p className="font-medium text-gray-600 text-sm">W czym mogę Państwu pomóc?</p>
            <p className="text-xs mt-1.5 text-gray-400">
              Proszę zapytać o adopcję, godziny otwarcia lub pomoc w wyborze zwierzęcia
            </p>
          </div>
        )}

        {messages.map((message) => (
          <ChatMessage key={message.id} message={message} />
        ))}

        {isLoading && <TypingIndicator />}

        {error && (
          <div
            className="mx-1 p-3 bg-red-50 border border-red-200 rounded-xl text-red-700 text-sm"
            role="alert"
          >
            <p className="font-medium text-xs">Wystapil blad</p>
            <p className="text-xs mt-1 text-red-600">{error}</p>
            <button
              onClick={clearError}
              className="text-xs underline mt-2 hover:text-red-800"
            >
              Zamknij
            </button>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div className="border-t border-gray-200 px-4 py-3 bg-white shrink-0">
        <div className="flex gap-2">
          <input
            ref={inputRef}
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Napisz wiadomosc..."
            className="flex-1 px-4 py-2.5 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm bg-gray-50 placeholder:text-gray-400"
            disabled={isLoading}
            aria-label="Wiadomosc do asystenta"
            maxLength={2000}
          />
          <button
            onClick={handleSubmit}
            disabled={!input.trim() || isLoading}
            className="px-3 py-2.5 bg-primary-600 text-white rounded-xl hover:bg-primary-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
            aria-label="Wyslij wiadomosc"
          >
            <PaperAirplaneIcon className="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  );
}

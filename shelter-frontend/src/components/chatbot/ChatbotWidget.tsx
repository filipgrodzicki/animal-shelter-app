import { ChatBubbleLeftRightIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { useChatbotStore } from '../../stores/chatbotStore';
import { ChatWindow } from './ChatWindow';

/**
 * Widget chatbota - przycisk i okno czatu
 * Wyświetlany w prawym dolnym rogu na wszystkich stronach
 */
export function ChatbotWidget() {
  const { isOpen, toggle } = useChatbotStore();

  return (
    <div className="fixed bottom-4 right-4 z-40">
      {/* Okno czatu */}
      {isOpen && <ChatWindow />}

      {/* Przycisk toggle */}
      <button
        onClick={toggle}
        className={`
          flex items-center justify-center w-14 h-14 rounded-full shadow-lg
          transition-all duration-300 ease-in-out
          focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2
          ${
            isOpen
              ? 'bg-gray-600 hover:bg-gray-700 rotate-0'
              : 'bg-primary-600 hover:bg-primary-700 hover:scale-105'
          }
          text-white
        `}
        aria-label={isOpen ? 'Zamknij czat' : 'Otworz czat z asystentem'}
        aria-expanded={isOpen}
        aria-controls="chatbot-window"
      >
        {isOpen ? (
          <XMarkIcon className="h-6 w-6" />
        ) : (
          <ChatBubbleLeftRightIcon className="h-6 w-6" />
        )}
      </button>
    </div>
  );
}

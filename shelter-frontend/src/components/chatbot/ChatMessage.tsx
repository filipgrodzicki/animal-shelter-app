import { Link, useNavigate } from 'react-router-dom';
import type { ChatMessage as ChatMessageType } from '../../stores/chatbotStore';
import { useAuth } from '@/context/AuthContext';

interface ChatMessageProps {
  message: ChatMessageType;
}

/**
 * Parses simple markdown text into React elements.
 * Supports: **bold**, ordered lists (1. ...), unordered lists (- / * ...)
 */
function renderContent(text: string, isUser: boolean) {
  const lines = text.split('\n');
  const elements: React.ReactNode[] = [];
  let listItems: { type: 'ul' | 'ol'; items: string[] } | null = null;

  const flushList = () => {
    if (!listItems) return;
    const items = listItems.items.map((item, i) => (
      <li key={i}>{formatInline(item)}</li>
    ));
    if (listItems.type === 'ol') {
      elements.push(
        <ol key={`ol-${elements.length}`} className={`list-decimal list-inside space-y-1 my-1 ${isUser ? 'text-white/90' : 'text-gray-700'}`}>
          {items}
        </ol>
      );
    } else {
      elements.push(
        <ul key={`ul-${elements.length}`} className={`list-disc list-inside space-y-1 my-1 ${isUser ? 'text-white/90' : 'text-gray-700'}`}>
          {items}
        </ul>
      );
    }
    listItems = null;
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const trimmed = line.trim();

    // Empty line
    if (trimmed === '') {
      flushList();
      continue;
    }

    // Ordered list: "1. text" / "2. text"
    const olMatch = trimmed.match(/^(\d+)\.\s+(.+)/);
    if (olMatch) {
      if (!listItems || listItems.type !== 'ol') {
        flushList();
        listItems = { type: 'ol', items: [] };
      }
      listItems.items.push(olMatch[2]);
      continue;
    }

    // Unordered list: "- text" / "* text"
    const ulMatch = trimmed.match(/^[-*]\s+(.+)/);
    if (ulMatch) {
      if (!listItems || listItems.type !== 'ul') {
        flushList();
        listItems = { type: 'ul', items: [] };
      }
      listItems.items.push(ulMatch[1]);
      continue;
    }

    // Plain text
    flushList();
    elements.push(
      <p key={`p-${i}`} className="my-0.5">
        {formatInline(trimmed)}
      </p>
    );
  }

  flushList();
  return elements;
}

/**
 * Formats inline elements: **bold**, [text](/link)
 */
function formatInline(text: string): React.ReactNode {
  // Handle markdown links [text](url) and **bold**
  const parts = text.split(/(\*\*[^*]+\*\*|\[[^\]]+\]\([^)]+\))/g);
  return parts.map((part, i) => {
    if (part.startsWith('**') && part.endsWith('**')) {
      return <strong key={i}>{part.slice(2, -2)}</strong>;
    }
    const linkMatch = part.match(/^\[([^\]]+)\]\(([^)]+)\)$/);
    if (linkMatch) {
      const [, linkText, url] = linkMatch;
      if (url.startsWith('/')) {
        return <Link key={i} to={url} className="text-primary-600 underline hover:text-primary-700">{linkText}</Link>;
      }
      return <a key={i} href={url} target="_blank" rel="noopener noreferrer" className="text-primary-600 underline hover:text-primary-700">{linkText}</a>;
    }
    return part;
  });
}

/**
 * Single chat message component
 */
export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === 'user';
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleAdoptClick = (animalId: string) => {
    if (isAuthenticated) {
      navigate(`/adoption/apply/${animalId}`);
    } else {
      navigate('/login', { state: { from: { pathname: `/adoption/apply/${animalId}` } } });
    }
  };

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[85%] px-4 py-2.5 rounded-2xl ${
          isUser
            ? 'bg-primary-600 text-white rounded-br-sm'
            : 'bg-white text-gray-800 rounded-bl-sm shadow-sm border border-gray-100'
        }`}
      >
        {/* Message content */}
        <div className="text-sm leading-relaxed break-words">
          {isUser ? (
            <p className="whitespace-pre-wrap">{message.content}</p>
          ) : (
            renderContent(message.content, false)
          )}
        </div>

        {/* Animal recommendations */}
        {message.recommendations && message.recommendations.length > 0 && (
          <div className="mt-3 space-y-2">
            {message.recommendations.map((rec) => (
              <div
                key={rec.id}
                className="p-3 bg-gray-50 rounded-xl border border-gray-200"
              >
                <Link
                  to={`/animals/${rec.id}`}
                  className="block hover:opacity-80 transition-opacity"
                >
                  <div className="flex items-center gap-3">
                    {rec.photoUrl ? (
                      <img
                        src={rec.photoUrl}
                        alt={rec.name}
                        className="w-11 h-11 rounded-lg object-cover flex-shrink-0"
                      />
                    ) : (
                      <div className="w-11 h-11 rounded-lg bg-primary-100 flex items-center justify-center flex-shrink-0">
                        <span className="text-primary-600 text-sm font-semibold">
                          {rec.name.charAt(0)}
                        </span>
                      </div>
                    )}
                    <div className="flex-1 min-w-0">
                      <p className="font-semibold text-sm text-gray-900 truncate">
                        {rec.name}
                      </p>
                      <p className="text-xs text-gray-500 truncate">
                        {rec.species === 'Dog' ? 'Pies' : 'Kot'} &bull; {rec.breed}
                      </p>
                    </div>
                    <div className="flex-shrink-0">
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-primary-50 text-primary-700">
                        {Math.round(rec.matchScore * 100)}%
                      </span>
                    </div>
                  </div>
                  <p className="mt-1.5 text-xs text-gray-500 line-clamp-2">
                    {rec.matchReason}
                  </p>
                </Link>
                <button
                  onClick={() => handleAdoptClick(rec.id)}
                  className="mt-2 block w-full text-center text-xs font-semibold py-1.5 px-3 rounded-lg bg-primary-600 text-white hover:bg-primary-700 transition-colors cursor-pointer"
                >
                  Adoptuj
                </button>
              </div>
            ))}
          </div>
        )}

        {/* Timestamp */}
        <p
          className={`text-[10px] mt-1.5 ${
            isUser ? 'text-primary-200' : 'text-gray-400'
          }`}
        >
          {message.timestamp.toLocaleTimeString('pl-PL', {
            hour: '2-digit',
            minute: '2-digit',
          })}
        </p>
      </div>
    </div>
  );
}

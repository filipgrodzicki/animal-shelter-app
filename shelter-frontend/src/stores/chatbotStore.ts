import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { chatbotApi, ChatMessageDto, AnimalRecommendationDto } from '../api/chatbot';

// ============ Types ============

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  recommendations?: AnimalRecommendationDto[];
}

interface ChatbotState {
  // State
  isOpen: boolean;
  sessionId: string | null;
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  anonymousSessionId: string;

  // Actions
  setOpen: (isOpen: boolean) => void;
  toggle: () => void;
  sendMessage: (message: string) => Promise<void>;
  startSession: () => Promise<void>;
  clearSession: () => void;
  clearError: () => void;
}

// ============ Helpers ============

function generateAnonymousId(): string {
  return 'anon-' + Math.random().toString(36).substring(2, 15);
}

function mapMessageFromDto(dto: ChatMessageDto): ChatMessage {
  return {
    id: dto.id,
    role: dto.role === 'user' ? 'user' : 'assistant',
    content: dto.content,
    timestamp: new Date(dto.timestamp),
    recommendations: dto.recommendations,
  };
}

function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }
  if (typeof error === 'string') {
    return error;
  }
  return 'Wystąpił nieoczekiwany błąd. Spróbuj ponownie.';
}

// ============ Store ============

export const useChatbotStore = create<ChatbotState>()(
  persist(
    (set, get) => ({
      // Initial state
      isOpen: false,
      sessionId: null,
      messages: [],
      isLoading: false,
      error: null,
      anonymousSessionId: generateAnonymousId(),

      // Actions
      setOpen: (isOpen) => set({ isOpen }),

      toggle: () => {
        const { isOpen, messages, startSession } = get();

        // If opening and no messages exist, start a new session
        // (also handles the case when session expired on server after rebuild)
        if (!isOpen && messages.length === 0) {
          set({ sessionId: null });
          startSession();
        }

        set({ isOpen: !isOpen });
      },

      startSession: async () => {
        const state = get();
        if (state.isLoading) return;

        set({ isLoading: true, error: null });

        try {
          const response = await chatbotApi.startSession();

          set({
            sessionId: response.sessionId,
            messages: response.messages.map(mapMessageFromDto),
            isLoading: false,
          });
        } catch (error) {
          set({
            error: getErrorMessage(error),
            isLoading: false,
          });
        }
      },

      sendMessage: async (message: string) => {
        const state = get();
        if (state.isLoading || !message.trim()) return;

        // If no session exists, create one first
        if (!state.sessionId) {
          await get().startSession();
        }

        // Add user message immediately (optimistic UI)
        const userMessage: ChatMessage = {
          id: 'temp-' + Date.now(),
          role: 'user',
          content: message.trim(),
          timestamp: new Date(),
        };

        set((state) => ({
          messages: [...state.messages, userMessage],
          isLoading: true,
          error: null,
        }));

        try {
          const response = await chatbotApi.sendMessage({
            message: message.trim(),
            sessionId: state.sessionId ?? undefined,
          });

          // Update session and add assistant response
          set((state) => ({
            sessionId: response.sessionId,
            messages: [
              // Remove temporary user message and add the real one with response
              ...state.messages.filter(m => m.id !== userMessage.id),
              { ...userMessage, id: 'user-' + Date.now() },
              mapMessageFromDto(response.assistantMessage),
            ],
            isLoading: false,
          }));
        } catch (error) {
          // Remove temporary message on error
          set((state) => ({
            messages: state.messages.filter(m => m.id !== userMessage.id),
            error: getErrorMessage(error),
            isLoading: false,
          }));
        }
      },

      clearSession: () => {
        set({
          sessionId: null,
          messages: [],
          error: null,
          anonymousSessionId: generateAnonymousId(),
        });
      },

      clearError: () => set({ error: null }),
    }),
    {
      name: 'chatbot-storage',
      partialize: (state) => ({
        sessionId: state.sessionId,
        anonymousSessionId: state.anonymousSessionId,
        // Don't persist messages - they will be fetched from the server
      }),
    }
  )
);

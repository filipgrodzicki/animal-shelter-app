import { post, get } from './client';

const BASE_URL = '/chatbot';

// ============ Types ============

export interface ChatMessageDto {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  recommendations?: AnimalRecommendationDto[];
}

export interface AnimalRecommendationDto {
  id: string;
  name: string;
  species: string;
  breed: string;
  photoUrl?: string;
  matchScore: number;
  matchReason: string;
}

export interface UserProfileDto {
  preferredSpecies?: string;
  experience?: string;
  livingConditions?: string;
  lifestyle?: string;
  hasChildren?: boolean;
  hasOtherPets?: boolean;
  sizePreference?: string;
}

export interface ChatSessionDto {
  sessionId: string;
  state: string;
  profile?: UserProfileDto;
  messages: ChatMessageDto[];
}

export interface SendMessageRequest {
  message: string;
  sessionId?: string;
}

export interface SendMessageResponse {
  sessionId: string;
  assistantMessage: ChatMessageDto;
  nextProfilingQuestion?: string;
}

// ============ API Functions ============

/**
 * Wysyła wiadomość do chatbota
 */
export async function sendMessage(request: SendMessageRequest): Promise<SendMessageResponse> {
  return post<SendMessageResponse>(`${BASE_URL}/message`, request);
}

/**
 * Rozpoczyna nową sesję czatu
 */
export async function startSession(): Promise<ChatSessionDto> {
  return post<ChatSessionDto>(`${BASE_URL}/session`);
}

/**
 * Pobiera istniejącą sesję czatu
 */
export async function getSession(sessionId: string): Promise<ChatSessionDto> {
  return get<ChatSessionDto>(`${BASE_URL}/session/${sessionId}`);
}

// Export as object for compatibility
export const chatbotApi = {
  sendMessage,
  startSession,
  getSession,
};

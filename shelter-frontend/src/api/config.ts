import { get, put } from './client';
import {
  SystemPromptConfig,
  MatchingWeightsConfig,
  FullAiConfig,
} from '@/types';

const BASE_URL = '/system-config';

export const configApi = {
  // Get system prompt configuration
  getSystemPrompt: async (): Promise<SystemPromptConfig> => {
    return get<SystemPromptConfig>(`${BASE_URL}/chatbot/prompt`);
  },

  // Update system prompt configuration
  updateSystemPrompt: async (data: SystemPromptConfig): Promise<SystemPromptConfig> => {
    return put<SystemPromptConfig>(`${BASE_URL}/chatbot/prompt`, data);
  },

  // Get matching weights configuration
  getMatchingWeights: async (): Promise<MatchingWeightsConfig> => {
    return get<MatchingWeightsConfig>(`${BASE_URL}/matching/weights`);
  },

  // Update matching weights configuration
  updateMatchingWeights: async (data: MatchingWeightsConfig): Promise<MatchingWeightsConfig> => {
    return put<MatchingWeightsConfig>(`${BASE_URL}/matching/weights`, data);
  },

  // Get full AI configuration
  getFullAiConfig: async (): Promise<FullAiConfig> => {
    return get<FullAiConfig>(`${BASE_URL}/ai`);
  },
};

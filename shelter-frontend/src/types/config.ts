// System configuration types

export interface SystemPromptConfig {
  role: string;
  allowedTopics: string[];
  rules: string[];
  fallbackMessage: string;
  offTopicMessage: string;
}

export interface MatchingWeightsConfig {
  experience: number;
  space: number;
  careTime: number;
  children: number;
  otherAnimals: number;
}

export interface FullAiConfig {
  systemPrompt: SystemPromptConfig;
  matchingWeights: MatchingWeightsConfig;
}

// Helper functions
export function getWeightLabel(key: string): string {
  const labels: Record<string, string> = {
    experience: 'Doswiadczenie',
    space: 'Przestrzen mieszkalna',
    careTime: 'Czas na opieke',
    children: 'Kompatybilnosc z dziecmi',
    otherAnimals: 'Kompatybilnosc z innymi zwierzetami',
  };
  return labels[key] || key;
}

export function getWeightDescription(key: string): string {
  const descriptions: Record<string, string> = {
    experience: 'Waga dla poziomu doswiadczenia adoptujacego ze zwierzetami',
    space: 'Waga dla wymagan przestrzennych (mieszkanie, dom z ogrodem)',
    careTime: 'Waga dla dostepnego czasu na opieke',
    children: 'Waga dla kompatybilnosci zwierzecia z dziecmi',
    otherAnimals: 'Waga dla kompatybilnosci z innymi zwierzetami w domu',
  };
  return descriptions[key] || '';
}

export type WeightKey = keyof MatchingWeightsConfig;

export const WEIGHT_KEYS: WeightKey[] = ['experience', 'space', 'careTime', 'children', 'otherAnimals'];

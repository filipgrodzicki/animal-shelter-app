import { useState, useEffect } from 'react';
import {
  ChatBubbleLeftRightIcon,
  ScaleIcon,
  PlusIcon,
  TrashIcon,
  CheckIcon,
  ExclamationTriangleIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Input, Spinner } from '@/components/common';
import { configApi } from '@/api/config';
import { getErrorMessage } from '@/api/client';
import {
  SystemPromptConfig,
  MatchingWeightsConfig,
  getWeightLabel,
  getWeightDescription,
  WEIGHT_KEYS,
  WeightKey,
} from '@/types';
import toast from 'react-hot-toast';

type Tab = 'chatbot' | 'matching';

export function AdminConfigPage() {
  const [activeTab, setActiveTab] = useState<Tab>('chatbot');

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Konfiguracja systemu</h1>
        <p className="mt-2 text-gray-600">Konfiguracja chatbota AI i algorytmu dopasowania zwierzat</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex gap-6">
          <button
            onClick={() => setActiveTab('chatbot')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'chatbot'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <ChatBubbleLeftRightIcon className="h-5 w-5" />
            Chatbot AI
          </button>
          <button
            onClick={() => setActiveTab('matching')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'matching'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <ScaleIcon className="h-5 w-5" />
            Dopasowanie zwierzat
          </button>
        </nav>
      </div>

      {/* Tab content */}
      {activeTab === 'chatbot' && <ChatbotConfig />}
      {activeTab === 'matching' && <MatchingConfig />}
    </PageContainer>
  );
}

// Chatbot Configuration Component
function ChatbotConfig() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<SystemPromptConfig>({
    role: '',
    allowedTopics: [],
    rules: [],
    fallbackMessage: '',
    offTopicMessage: '',
  });
  const [newTopic, setNewTopic] = useState('');
  const [newRule, setNewRule] = useState('');

  useEffect(() => {
    fetchConfig();
  }, []);

  const fetchConfig = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await configApi.getSystemPrompt();
      setConfig(data);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  const handleSave = async () => {
    if (!config.role.trim()) {
      toast.error('Rola chatbota jest wymagana');
      return;
    }

    setIsSaving(true);
    try {
      await configApi.updateSystemPrompt(config);
      toast.success('Konfiguracja chatbota zostala zapisana');
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSaving(false);
    }
  };

  const addTopic = () => {
    if (newTopic.trim() && !config.allowedTopics.includes(newTopic.trim())) {
      setConfig({
        ...config,
        allowedTopics: [...config.allowedTopics, newTopic.trim()],
      });
      setNewTopic('');
    }
  };

  const removeTopic = (topic: string) => {
    setConfig({
      ...config,
      allowedTopics: config.allowedTopics.filter((t) => t !== topic),
    });
  };

  const addRule = () => {
    if (newRule.trim() && !config.rules.includes(newRule.trim())) {
      setConfig({
        ...config,
        rules: [...config.rules, newRule.trim()],
      });
      setNewRule('');
    }
  };

  const removeRule = (rule: string) => {
    setConfig({
      ...config,
      rules: config.rules.filter((r) => r !== rule),
    });
  };

  if (isLoading) {
    return (
      <div className="p-8 flex justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center text-red-600">{error}</div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Role */}
      <Card className="p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Rola chatbota</h3>
        <p className="text-sm text-gray-500 mb-3">
          Okresl glowna role i osobowosc chatbota. Ten tekst jest uzywany jako "system prompt".
        </p>
        <textarea
          value={config.role}
          onChange={(e) => setConfig({ ...config, role: e.target.value })}
          rows={4}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
          placeholder="Jestes asystentem schroniska dla zwierzat..."
        />
      </Card>

      {/* Allowed Topics */}
      <Card className="p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Dozwolone tematy</h3>
        <p className="text-sm text-gray-500 mb-3">
          Lista tematow, na ktore chatbot moze odpowiadac.
        </p>
        <div className="flex gap-2 mb-4">
          <Input
            value={newTopic}
            onChange={(e) => setNewTopic(e.target.value)}
            placeholder="Nowy temat..."
            onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addTopic())}
            className="flex-1"
          />
          <Button onClick={addTopic} leftIcon={<PlusIcon className="h-5 w-5" />}>
            Dodaj
          </Button>
        </div>
        <div className="flex flex-wrap gap-2">
          {config.allowedTopics.map((topic) => (
            <span
              key={topic}
              className="inline-flex items-center gap-1 px-3 py-1 bg-blue-50 text-blue-700 rounded-full text-sm"
            >
              {topic}
              <button
                onClick={() => removeTopic(topic)}
                className="hover:text-blue-900"
              >
                <TrashIcon className="h-4 w-4" />
              </button>
            </span>
          ))}
          {config.allowedTopics.length === 0 && (
            <span className="text-gray-400 text-sm">Brak tematow</span>
          )}
        </div>
      </Card>

      {/* Rules */}
      <Card className="p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Reguly chatbota</h3>
        <p className="text-sm text-gray-500 mb-3">
          Reguly i wytyczne, ktorych chatbot powinien przestrzegac.
        </p>
        <div className="flex gap-2 mb-4">
          <Input
            value={newRule}
            onChange={(e) => setNewRule(e.target.value)}
            placeholder="Nowa regula..."
            onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addRule())}
            className="flex-1"
          />
          <Button onClick={addRule} leftIcon={<PlusIcon className="h-5 w-5" />}>
            Dodaj
          </Button>
        </div>
        <ul className="space-y-2">
          {config.rules.map((rule, index) => (
            <li
              key={index}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <span className="text-sm text-gray-700">{rule}</span>
              <button
                onClick={() => removeRule(rule)}
                className="text-gray-400 hover:text-red-600"
              >
                <TrashIcon className="h-5 w-5" />
              </button>
            </li>
          ))}
          {config.rules.length === 0 && (
            <li className="text-gray-400 text-sm p-3">Brak regul</li>
          )}
        </ul>
      </Card>

      {/* Messages */}
      <Card className="p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Wiadomosci systemowe</h3>

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Wiadomosc fallback
            </label>
            <p className="text-xs text-gray-500 mb-2">
              Wyswietlana gdy chatbot nie jest w stanie odpowiedziec.
            </p>
            <textarea
              value={config.fallbackMessage}
              onChange={(e) => setConfig({ ...config, fallbackMessage: e.target.value })}
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              placeholder="Przepraszam, nie jestem w stanie odpowiedziec na to pytanie..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Wiadomosc off-topic
            </label>
            <p className="text-xs text-gray-500 mb-2">
              Wyswietlana gdy uzytkownik pyta o tematy spoza zakresu.
            </p>
            <textarea
              value={config.offTopicMessage}
              onChange={(e) => setConfig({ ...config, offTopicMessage: e.target.value })}
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              placeholder="Moge pomoc tylko w sprawach zwiazanych ze schroniskiem..."
            />
          </div>
        </div>
      </Card>

      {/* Save Button */}
      <div className="flex justify-end">
        <Button
          onClick={handleSave}
          isLoading={isSaving}
          leftIcon={<CheckIcon className="h-5 w-5" />}
          size="lg"
        >
          Zapisz konfiguracje
        </Button>
      </div>
    </div>
  );
}

// Matching Configuration Component
function MatchingConfig() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [weights, setWeights] = useState<MatchingWeightsConfig>({
    experience: 0.30,
    space: 0.20,
    careTime: 0.20,
    children: 0.15,
    otherAnimals: 0.15,
  });

  useEffect(() => {
    fetchWeights();
  }, []);

  const fetchWeights = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await configApi.getMatchingWeights();
      setWeights(data);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  const handleSave = async () => {
    const sum = Object.values(weights).reduce((a, b) => a + b, 0);
    if (Math.abs(sum - 1) > 0.01) {
      toast.error('Suma wag musi wynosic 100%');
      return;
    }

    setIsSaving(true);
    try {
      await configApi.updateMatchingWeights(weights);
      toast.success('Wagi dopasowania zostaly zapisane');
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSaving(false);
    }
  };

  const updateWeight = (key: WeightKey, value: number) => {
    setWeights({
      ...weights,
      [key]: value,
    });
  };

  const totalPercentage = Object.values(weights).reduce((a, b) => a + b, 0) * 100;
  const isValidSum = Math.abs(totalPercentage - 100) <= 1;

  if (isLoading) {
    return (
      <div className="p-8 flex justify-center">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center text-red-600">{error}</div>
    );
  }

  return (
    <div className="space-y-6">
      <Card className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h3 className="text-lg font-medium text-gray-900">Wagi algorytmu dopasowania</h3>
            <p className="text-sm text-gray-500 mt-1">
              Okresl waznosc poszczegolnych kryteriow przy dopasowywaniu zwierzat do adoptujacych.
            </p>
          </div>
          <div className={`px-4 py-2 rounded-lg ${isValidSum ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
            <div className="flex items-center gap-2">
              {isValidSum ? (
                <CheckIcon className="h-5 w-5" />
              ) : (
                <ExclamationTriangleIcon className="h-5 w-5" />
              )}
              <span className="font-medium">{totalPercentage.toFixed(0)}%</span>
            </div>
          </div>
        </div>

        <div className="space-y-6">
          {WEIGHT_KEYS.map((key) => (
            <WeightSlider
              key={key}
              label={getWeightLabel(key)}
              description={getWeightDescription(key)}
              value={weights[key]}
              onChange={(value) => updateWeight(key, value)}
            />
          ))}
        </div>

        {!isValidSum && (
          <div className="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
            <div className="flex items-center gap-2 text-yellow-700">
              <ExclamationTriangleIcon className="h-5 w-5" />
              <span>Suma wag musi wynosic dokladnie 100%. Aktualna suma: {totalPercentage.toFixed(0)}%</span>
            </div>
          </div>
        )}
      </Card>

      {/* Explanation */}
      <Card className="p-6 bg-blue-50 border-blue-200">
        <h4 className="font-medium text-blue-900 mb-2">Jak dzialaja wagi?</h4>
        <p className="text-sm text-blue-700">
          Algorytm dopasowania wykorzystuje te wagi do obliczenia wyniku kompatybilnosci miedzy
          adoptujacym a zwierzeciem. Wyzsza waga oznacza, ze dane kryterium ma wiekszy wplyw
          na koncowy wynik dopasowania. Suma wszystkich wag musi wynosic 100%.
        </p>
      </Card>

      {/* Save Button */}
      <div className="flex justify-end">
        <Button
          onClick={handleSave}
          isLoading={isSaving}
          disabled={!isValidSum}
          leftIcon={<CheckIcon className="h-5 w-5" />}
          size="lg"
        >
          Zapisz wagi
        </Button>
      </div>
    </div>
  );
}

// Weight Slider Component
interface WeightSliderProps {
  label: string;
  description: string;
  value: number;
  onChange: (value: number) => void;
}

function WeightSlider({ label, description, value, onChange }: WeightSliderProps) {
  const percentage = Math.round(value * 100);

  return (
    <div className="flex items-center justify-between py-3 border-b border-gray-100 last:border-0">
      <div className="flex-1">
        <label className="font-medium text-gray-900">{label}</label>
        <p className="text-xs text-gray-500">{description}</p>
      </div>
      <div className="flex items-center gap-2">
        <input
          type="number"
          min="0"
          max="100"
          value={percentage}
          onChange={(e) => onChange(Number(e.target.value) / 100)}
          className="w-20 px-3 py-2 text-right border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
        />
        <span className="text-gray-500 font-medium">%</span>
      </div>
    </div>
  );
}

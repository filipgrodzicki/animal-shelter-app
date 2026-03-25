import {
  BeakerIcon,
  CalendarIcon,
  UserIcon,
  DocumentTextIcon,
  PaperClipIcon,
  ArrowDownTrayIcon,
} from '@heroicons/react/24/outline';
import { Card, Badge } from '@/components/common';
import { MedicalRecord, MedicalRecordAttachment, getMedicalRecordTypeLabel } from '@/types';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';

interface AnimalMedicalHistoryProps {
  records: MedicalRecord[];
  showAll?: boolean;
}

type BadgeColor = 'green' | 'blue' | 'yellow' | 'red' | 'gray' | 'purple';

const recordTypeColors: Record<string, BadgeColor> = {
  Vaccination: 'green',
  Treatment: 'blue',
  Surgery: 'red',
  Checkup: 'gray',
  Deworming: 'purple',
  Sterilization: 'yellow',
  Chipping: 'blue',
  Other: 'gray',
};

export function AnimalMedicalHistory({
  records,
  showAll = false,
}: AnimalMedicalHistoryProps) {
  if (records.length === 0) {
    return null;
  }

  // Sort by date (newest first) and limit if not showing all
  const sortedRecords = [...records]
    .sort((a, b) => new Date(b.recordDate).getTime() - new Date(a.recordDate).getTime());

  const displayedRecords = showAll ? sortedRecords : sortedRecords.slice(0, 5);

  return (
    <Card className="overflow-hidden">
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
            <BeakerIcon className="h-5 w-5 text-gray-400" />
            Historia medyczna
          </h3>
          <span className="text-sm text-gray-500">
            {records.length} {records.length === 1 ? 'wpis' : records.length < 5 ? 'wpisy' : 'wpisów'}
          </span>
        </div>

        <div className="space-y-4">
          {displayedRecords.map((record) => (
            <MedicalRecordItem key={record.id} record={record} />
          ))}
        </div>

        {!showAll && records.length > 5 && (
          <div className="mt-4 pt-4 border-t border-gray-100 text-center">
            <p className="text-sm text-gray-500">
              Pokazano {displayedRecords.length} z {records.length} wpisów
            </p>
          </div>
        )}
      </div>
    </Card>
  );
}

interface MedicalRecordItemProps {
  record: MedicalRecord;
}

function MedicalRecordItem({ record }: MedicalRecordItemProps) {
  // Backward compatibility: use type or recordType
  const recordType = record.type || record.recordType || 'Other';
  const badgeColor = recordTypeColors[recordType] || 'gray';
  const veterinarian = record.veterinarianName || record.veterinarian;
  const nextVisit = record.nextVisitDate || record.nextAppointmentDate;

  return (
    <div className="relative pl-6 pb-4 border-l-2 border-gray-200 last:pb-0 last:border-l-transparent">
      {/* Timeline dot */}
      <div className="absolute left-0 top-0 w-3 h-3 -translate-x-[7px] rounded-full bg-white border-2 border-primary-500" />

      <div className="bg-gray-50 rounded-lg p-4">
        {/* Header */}
        <div className="flex items-start justify-between gap-2 mb-2">
          <Badge color={badgeColor} size="sm">
            {getMedicalRecordTypeLabel(recordType)}
          </Badge>
          <span className="text-sm text-gray-500 flex items-center gap-1">
            <CalendarIcon className="h-4 w-4" />
            {format(new Date(record.recordDate), 'd MMM yyyy', { locale: pl })}
          </span>
        </div>

        {/* Title */}
        {record.title && (
          <h4 className="font-medium text-gray-900 mb-1">{record.title}</h4>
        )}

        {/* Description */}
        <p className="text-gray-700 mb-2">{record.description}</p>

        {/* Diagnosis, treatment, medications */}
        {record.diagnosis && (
          <p className="text-sm text-gray-600 mb-1">
            <span className="font-medium">Diagnoza:</span> {record.diagnosis}
          </p>
        )}
        {record.treatment && (
          <p className="text-sm text-gray-600 mb-1">
            <span className="font-medium">Leczenie:</span> {record.treatment}
          </p>
        )}
        {record.medications && (
          <p className="text-sm text-gray-600 mb-1">
            <span className="font-medium">Leki:</span> {record.medications}
          </p>
        )}

        {/* Additional info */}
        <div className="flex flex-wrap gap-4 text-sm text-gray-500 mt-2">
          {veterinarian && (
            <span className="flex items-center gap-1">
              <UserIcon className="h-4 w-4" />
              {veterinarian}
            </span>
          )}
          {nextVisit && (
            <span className="flex items-center gap-1 text-primary-600">
              <CalendarIcon className="h-4 w-4" />
              Następna wizyta: {format(new Date(nextVisit), 'd MMM yyyy', { locale: pl })}
            </span>
          )}
          {record.cost !== undefined && record.cost !== null && (
            <span className="text-gray-500">
              Koszt: {record.cost.toFixed(2)} zł
            </span>
          )}
        </div>

        {/* WF-06: Entered by info */}
        {record.enteredBy && (
          <div className="mt-2 text-xs text-gray-400">
            Wprowadził: {record.enteredBy}
          </div>
        )}

        {/* Notes */}
        {record.notes && (
          <div className="mt-3 pt-3 border-t border-gray-200">
            <p className="text-sm text-gray-600 flex items-start gap-1">
              <DocumentTextIcon className="h-4 w-4 flex-shrink-0 mt-0.5" />
              {record.notes}
            </p>
          </div>
        )}

        {/* WF-06: Attachments */}
        {record.attachments && record.attachments.length > 0 && (
          <div className="mt-3 pt-3 border-t border-gray-200">
            <p className="text-sm font-medium text-gray-700 flex items-center gap-1 mb-2">
              <PaperClipIcon className="h-4 w-4" />
              Załączniki ({record.attachments.length})
            </p>
            <div className="space-y-1">
              {record.attachments.map((attachment) => (
                <AttachmentItem key={attachment.id} attachment={attachment} />
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// WF-06: Attachment item component
interface AttachmentItemProps {
  attachment: MedicalRecordAttachment;
}

function AttachmentItem({ attachment }: AttachmentItemProps) {
  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <a
      href={attachment.url}
      target="_blank"
      rel="noopener noreferrer"
      className="flex items-center gap-2 p-2 bg-white rounded border border-gray-200 hover:border-primary-300 hover:bg-primary-50 transition-colors text-sm"
    >
      <ArrowDownTrayIcon className="h-4 w-4 text-gray-400" />
      <span className="flex-1 truncate text-gray-700">{attachment.fileName}</span>
      <span className="text-xs text-gray-400">{formatFileSize(attachment.fileSize)}</span>
    </a>
  );
}

// Summary card for quick medical info
interface MedicalSummaryProps {
  records: MedicalRecord[];
}

export function MedicalSummary({ records }: MedicalSummaryProps) {
  // Backward compatibility: use type or recordType
  const getRecordType = (r: MedicalRecord) => r.type || r.recordType || 'Other';

  const vaccinations = records.filter((r) => getRecordType(r) === 'Vaccination');
  const lastCheckup = records
    .filter((r) => getRecordType(r) === 'Checkup' || getRecordType(r) === 'Examination')
    .sort((a, b) => new Date(b.recordDate).getTime() - new Date(a.recordDate).getTime())[0];
  const isSterilized = records.some((r) => getRecordType(r) === 'Sterilization');
  const isChipped = records.some((r) => getRecordType(r) === 'Chipping' || getRecordType(r) === 'Microchipping');

  return (
    <Card className="overflow-hidden">
      <div className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Status zdrowotny</h3>

        <div className="grid grid-cols-2 gap-4">
          <SummaryItem
            label="Szczepienia"
            value={vaccinations.length > 0 ? `${vaccinations.length} wykonane` : 'Brak danych'}
            positive={vaccinations.length > 0}
          />
          <SummaryItem
            label="Sterylizacja"
            value={isSterilized ? 'Tak' : 'Nie'}
            positive={isSterilized}
          />
          <SummaryItem
            label="Czipowanie"
            value={isChipped ? 'Tak' : 'Nie'}
            positive={isChipped}
          />
          <SummaryItem
            label="Ostatnie badanie"
            value={
              lastCheckup
                ? format(new Date(lastCheckup.recordDate), 'd MMM yyyy', { locale: pl })
                : 'Brak danych'
            }
            positive={!!lastCheckup}
          />
        </div>
      </div>
    </Card>
  );
}

interface SummaryItemProps {
  label: string;
  value: string;
  positive: boolean;
}

function SummaryItem({ label, value, positive }: SummaryItemProps) {
  return (
    <div className="p-3 bg-gray-50 rounded-lg">
      <p className="text-xs text-gray-500 mb-1">{label}</p>
      <p className={`font-medium ${positive ? 'text-green-600' : 'text-gray-600'}`}>
        {value}
      </p>
    </div>
  );
}

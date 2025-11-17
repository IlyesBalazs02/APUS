
export interface EditActivityDto {
    id: string;
    title: string;
    description?: string | null;
    date: string;
    activityType: string;   // "Running", "Cycling", ...
}
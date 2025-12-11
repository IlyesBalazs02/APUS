export interface profiledto {
    name: string;
}

export type TrainingPeriod = 'LastWeek' | 'LastMonth' | 'LastYear';

export interface TrainingSportSummaryDto {
    activityType: string;
    totalHours: number;
    activityCount: number;
}

export interface TrainingTimeSummaryDto {
    userId: string;
    period: TrainingPeriod;
    fromUtc: string;
    toUtc: string;
    totalHours: number;
    activityCount: number;
    sports: TrainingSportSummaryDto[];
}

export interface ActivityCalendarDayDto {
    day: number;          // ISO string (e.g. "2025-12-11T00:00:00Z")
    totalHours: number;
    activityCount: number;
}

export interface ActivityCalendarMonthDto {
    userId: string;
    year: number;
    month: number;         // 1–12
    days: ActivityCalendarDayDto[];
}
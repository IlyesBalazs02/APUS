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
    day: number;
    totalHours: number;
    activityCount: number;
}

export interface ActivityCalendarMonthDto {
    userId: string;
    year: number;
    month: number;
    days: ActivityCalendarDayDto[];
}
export interface MainActivity {
    $type: string;
    Time: number;
    HeartRate: number;
    Date: string;
}

export interface Running extends MainActivity {
    Pace: number;
    Distance: number;
}

export interface Bouldering extends MainActivity {

    Difficulty: number;
    RedPoint: number;
}

enum SportType {
    Running = "Running",
    Bouldering = "Bouldering"
}

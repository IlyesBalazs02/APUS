import { ActivityImage } from "./ActivityImage";
import { Coordinate } from "./Coordinate";

export class MainActivity {
    $type!: string;
    id: string = '';
    title: string = '';
    description: string | null = null;
    date: string = new Date().toISOString();
    duration: string | null = null;
    calories: number | null = null;
    avgHeartRate: number | null = null;
    maxHeartRate: number | null = null;
    displayName: string;
    activityType: string = '';
    activityImages: ActivityImage[] = [];
    coordinates: Coordinate[] = [];
    showCoordinates: boolean = false;


    constructor() {
        this.$type = 'APUS.Server.Models.Activities.MainActivity, APUS.Server';
        this.displayName = 'Activity';
    }
}

export class Running extends MainActivity {
    pace: number | null = null;
    distance: number | null = null;
    elevationGain: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Running, APUS.Server';
        this.displayName = 'Running';
        this.activityType = 'Running';
    }
}

export class Bouldering extends MainActivity {
    difficulty: number | null = null;
    redPoint: boolean | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Bouldering, APUS.Server';
        this.displayName = 'Bouldering';
    }
}

export class RockClimbing extends MainActivity {
    difficulty: number | null = null;
    elevationGain: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.RockClimbing, APUS.Server';
        this.displayName = 'Rock Climbing';
    }
}

export class Hiking extends MainActivity {
    distance: number | null = null;
    elevationGain: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Hiking, APUS.Server';
        this.displayName = 'Hiking';
    }
}

import { ActivityImage } from "./ActivityImage";
import { Coordinate } from "./Coordinate";

// ../fromly/newFields
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
    displayName: string | null = null;
    activityType: string = '';
    //activityImages: ActivityImage[] = [];
    //coordinates: Coordinate[] = [];
    //showCoordinates: boolean = false;


    constructor() {
        this.$type = 'APUS.Server.Models.MainActivity, APUS.Server';
        this.displayName = 'Activity';
    }
}

export class GpsRelatedActivity extends MainActivity {
    avgpace: number | null = null;
    totaldistancekm: number | null = null;
    totalascentmeters: number | null = null;
    totaldescentmeters: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.GpsRelatedActivity, APUS.Server';
    }
}

export class Running extends GpsRelatedActivity {

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Running, APUS.Server';
        this.displayName = 'Running';
        this.activityType = 'Running';
    }
}

export class Bouldering extends MainActivity {
    difficulty: number | null = null;
    //redPoint: boolean | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Bouldering, APUS.Server';
        this.displayName = 'Bouldering';
        this.activityType = 'Bouldering';

    }
}

export class RockClimbing extends MainActivity {
    difficulty: number | null = null;
    elevationGain: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.RockClimbing, APUS.Server';
        this.displayName = 'Rock Climbing';
        this.activityType = 'RockClimbing';

    }
}

export class Hiking extends GpsRelatedActivity {

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Hiking, APUS.Server';
        this.displayName = 'Hiking';
        this.activityType = 'Hiking';

    }
}

export class Yoga extends MainActivity {
    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Yoga, APUS.Server';
        this.displayName = 'Yoga';
        this.activityType = 'Yoga';

    }
}

export class Football extends MainActivity {
    distance: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Football, APUS.Server';
        this.displayName = 'Football';
        this.activityType = 'Football';

    }
}

export class Walk extends GpsRelatedActivity {

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Walk, APUS.Server';
        this.displayName = 'Walk';
        this.activityType = 'Walk';

    }
}

export class Ride extends GpsRelatedActivity {

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Ride, APUS.Server';
        this.displayName = 'Ride';
        this.activityType = 'Ride';

    }
}

export class Swimming extends MainActivity {
    distance: number | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Swimming, APUS.Server';
        this.displayName = 'Swimming';
        this.activityType = 'Swimming';

    }
}

export class Ski extends GpsRelatedActivity {

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Ski, APUS.Server';
        this.displayName = 'Ski';
        this.activityType = 'Ski';

    }
}

export class Tennis extends MainActivity {
    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Tennis, APUS.Server';
        this.displayName = 'Tennis';
        this.activityType = 'Tennis';

    }
}

const activityRegistry = new Map<string, new () => MainActivity>(
    [
        Running,
        Bouldering,
        RockClimbing,
        Hiking,
        Yoga,
        Football,
        Walk,
        Ride,
        Swimming,
        Ski,
        Tennis,
    ].map(ctor => {
        const tmp = new ctor();
        return [tmp.$type, ctor] as [string, new () => MainActivity];
    })
);

export function createActivity(dto: MainActivity): MainActivity {
    const Ctor = activityRegistry.get(dto.$type) ?? MainActivity;
    return Object.assign(new Ctor(), dto);
}
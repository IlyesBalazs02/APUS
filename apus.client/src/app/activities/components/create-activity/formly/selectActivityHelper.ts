import { FormlyFieldConfig } from "@ngx-formly/core";
import * as activities from "../../../_models/ActivityClasses";
import * as Fields from "./formFieldConfigs"

export class selectActivityHelper {

    SportTypes: activities.MainActivity[] = [
        new activities.MainActivity(),
        new activities.Running(),
        new activities.Bouldering(),
        new activities.RockClimbing(),
        new activities.Hiking(),
        new activities.Yoga(),
        new activities.Football(),
        new activities.Walk(),
        new activities.Ride(),
        new activities.Swimming(),
        new activities.Ski(),
        new activities.Tennis()
    ];
    selectedActivity: activities.MainActivity = this.SportTypes[0];

    subtypeMap: Record<string, FormlyFieldConfig[]> = {
        MainActivity: Fields.mainActivityFields,
        Running: Fields.runningFields,
        Bouldering: Fields.boulderingFields,
        RockClimbing: Fields.rockClimbingFields,
        Hiking: Fields.hikingFields,
        Yoga: Fields.yogaFields,
        Football: Fields.footballFields,
        Walk: Fields.walkFields,
        Ride: Fields.rideFields,
        Swimming: Fields.swimmingFields,
        Ski: Fields.skiFields,
        Tennis: Fields.tennisFields,
    };
}
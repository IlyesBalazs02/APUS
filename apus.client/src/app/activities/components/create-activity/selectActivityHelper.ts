import { FormlyFieldConfig } from "@ngx-formly/core";
import { Bouldering, MainActivity, Running } from "../../_models/ActivityClasses";
import { runningFields } from "../../formly/running.fields";

export class selectActivityHelper {
    selectedActivity: MainActivity = new MainActivity(); // Default to MainActivity

    // Activity Types For Creator
    get runningActivity(): Running {
        return (this.selectedActivity as Running);
    }

    get boulderingActivity(): Bouldering {
        return (this.selectedActivity as Bouldering);
    }

    SportTypes: MainActivity[] = [
        new MainActivity(),
        new Running(),
        new Bouldering()
    ];

    subtypeMap: Record<string, FormlyFieldConfig[]> = {
        Running: runningFields,
        // …etc
    };
}
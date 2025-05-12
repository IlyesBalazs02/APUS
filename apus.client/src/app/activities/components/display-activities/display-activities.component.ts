import { Component, OnInit } from '@angular/core';
import { MainActivity, createActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ActivityDto } from '../../ActivityDto/ActivityDto';
import { ActivityService } from '../../../services/activityService';

@Component({
  selector: 'app-display-activities',
  standalone: false,
  templateUrl: './display-activities.component.html',
  styleUrl: './display-activities.component.css'
})

export class DisplayActivitiesComponent implements OnInit {
  activities: ActivityDto[] = [];

  constructor(private activityService: ActivityService) { }

  ngOnInit(): void {
    this.activityService
      .getActivitiesDto()
      .subscribe((dtos: ActivityDto[]) => {
        this.activities = dtos;
        //console.log(this.activities);
      });

  }
}

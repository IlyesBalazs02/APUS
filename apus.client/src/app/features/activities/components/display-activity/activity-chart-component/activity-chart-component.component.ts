import { Component, Input, OnInit } from '@angular/core';
import { Trackpoint } from '../../../ActivityDto/TrackpointDto';

@Component({
  selector: 'app-activity-chart-component',
  standalone: false,
  templateUrl: './activity-chart-component.component.html',
  styleUrl: './activity-chart-component.component.css'
})
export class ActivityChartComponentComponent implements OnInit {
  @Input() trackpoints: Trackpoint[] = [];

  ngOnInit(): void {

  }
}

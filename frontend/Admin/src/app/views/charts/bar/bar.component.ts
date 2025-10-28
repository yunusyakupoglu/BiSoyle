import { ChartOptions } from '@/app/common/apexchart.model'
import { PageTitleComponent } from '@/app/components/page-title.component'
import { UIExamplesListComponent } from '@/app/components/ui-examples-list/ui-examples-list.component'
import { Component } from '@angular/core'
import { NgApexchartsModule } from 'ng-apexcharts'

@Component({
  selector: 'app-bar',
  standalone: true,
  imports: [PageTitleComponent, NgApexchartsModule, UIExamplesListComponent],
  templateUrl: './bar.component.html',
  styles: ``,
})
export class BarComponent {
  barChart: Partial<ChartOptions> = {
    chart: {
      height: 380,
      type: 'bar',
      toolbar: {
        show: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
      },
    },
    dataLabels: {
      enabled: false,
    },
    series: [
      {
        data: [455, 435, 410, 480, 530, 575, 685, 1345, 1165, 1075],
      },
    ],
    colors: ['#1c84ee'],
    xaxis: {
      categories: [
        'South Korea',
        'Canada',
        'United Kingdom',
        'Netherlands',
        'Italy',
        'France',
        'Japan',
        'United States',
        'China',
        'Germany',
      ],
    },
    states: {
      hover: {},
    },
    grid: {
      borderColor: '#f1f3fa',
    },
  }

  // Group bar chart
  groupedChart: Partial<ChartOptions> = {
    chart: {
      height: 380,
      type: 'bar',
      toolbar: {
        show: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
        dataLabels: {
          position: 'top',
        },
      },
    },
    dataLabels: {
      enabled: true,
      offsetX: -6,
      style: {
        fontSize: '12px',
        colors: ['#fff'],
      },
    },
    colors: ['#1c84ee', '#4ecac2'],
    stroke: {
      show: true,
      width: 1,
      colors: ['#fff'],
    },
    series: [
      {
        name: 'Series 1',
        data: [51, 30, 31, 50, 11, 42, 30],
      },
      {
        name: 'Series 2',
        data: [46, 57, 43, 66, 24, 45, 23],
      },
    ],
    xaxis: {
      categories: [2016, 2017, 2018, 2019, 2021, 2022, 2023],
    },
    legend: {
      offsetY: 5,
    },
    states: {
      hover: {},
    },
    grid: {
      borderColor: '#f1f3fa',
      padding: {
        bottom: 5,
      },
    },
  }

  // Group bar chart
  stackedChart: Partial<ChartOptions> = {
    chart: {
      height: 380,
      type: 'bar',
      stacked: true,
      toolbar: {
        show: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
      },
    },
    stroke: {
      show: false,
    },
    series: [
      {
        name: 'Marine Sprite',
        data: [30, 17, 24, 37, 30, 29, 15],
      },
      {
        name: 'Striking Calf',
        data: [11, 9, 7, 10, 8, 11, 6],
      },
      {
        name: 'Tank Picture',
        data: [14, 19, 13, 11, 17, 13, 22],
      },
      {
        name: 'Bucket Slope',
        data: [55, 34, 35, 54, 15, 45, 34],
      },
      {
        name: 'Reborn Kid',
        data: [46, 57, 43, 39, 24, 45, 23],
      },
    ],
    xaxis: {
      categories: [2016, 2017, 2018, 2019, 2021, 2022, 2023],
      labels: {
        formatter: function (val) {
          return val + 'K'
        },
      },
    },
    yaxis: {
      title: {
        text: undefined,
      },
    },
    colors: ['#1c84ee', '#4ecac2', '#22c55e', '#f9b931', '#ff6c2f'],
    tooltip: {
      y: {
        formatter: function (val) {
          return val + 'K'
        },
      },
    },
    fill: {
      opacity: 1,
    },
    states: {
      hover: {},
    },
    legend: {
      position: 'top',
      horizontalAlign: 'center',
      offsetY: 10,
    },
    grid: {
      borderColor: '#f1f3fa',
    },
  }

  // full stacked bar chart
  fullstackedChart: Partial<ChartOptions> = {
    chart: {
      height: 380,
      type: 'bar',
      stacked: true,
      stackType: '100%',
      toolbar: {
        show: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
      },
    },
    stroke: {
      width: 1,
      colors: ['#fff'],
    },
    series: [
      {
        name: 'Marine Sprite',
        data: [30, 17, 24, 37, 30, 29, 15],
      },
      {
        name: 'Striking Calf',
        data: [11, 9, 7, 10, 8, 11, 6],
      },
      {
        name: 'Tank Picture',
        data: [14, 19, 13, 11, 17, 13, 22],
      },
      {
        name: 'Bucket Slope',
        data: [55, 34, 35, 54, 15, 45, 34],
      },
      {
        name: 'Reborn Kid',
        data: [46, 57, 43, 39, 24, 45, 23],
      },
    ],
    xaxis: {
      categories: [2008, 2009, 2010, 2011, 2012, 2013, 2014],
    },
    colors: ['#1c84ee', '#4ecac2', '#22c55e', '#f9b931', '#ff6c2f'],
    tooltip: {
      y: {
        formatter: function (val) {
          return val + 'K'
        },
      },
    },
    fill: {
      opacity: 1,
    },
    states: {
      hover: {
        filter: {
          type: 'none',
        },
      },
    },
    legend: {
      position: 'top',
      horizontalAlign: 'center',
      offsetY: 10,
    },
    grid: {
      borderColor: '#f1f3fa',
      padding: {
        top: 0,
      },
    },
  }

  // Negative Bar Chart

  negativeChart: Partial<ChartOptions> = {
    chart: {
      height: 380,
      type: 'bar',
      stacked: true,
      toolbar: {
        show: false,
      },
    },
    colors: ['#1c84ee', '#4ecac2'],
    plotOptions: {
      bar: {
        horizontal: true,
        barHeight: '80%',
      },
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      width: 1,
      colors: ['#fff'],
    },
    series: [
      {
        name: 'Males',
        data: [
          0.75, 0.85, 0.96, 1.08, 1.7, 2.3, 3.1, 4.0, 4.1, 4.4, 4.2, 4.5, 4.3,
          4.4, 4.7, 4.1, 3.7, 3.2,
        ],
      },
      {
        name: 'Females',
        data: [
          -0.75, -0.85, -0.86, -0.98, -1.2, -2.0, -2.65, -3.5, -3.76, -4.02,
          -4.1, -4.2, -3.9, -3.8, -3.9, -3.2, -2.9, -2.6,
        ],
      },
    ],
    yaxis: {
      min: -5,
      max: 5,
      title: {
        // text: 'Age',
      },
    },
    tooltip: {
      shared: false,
      x: {
        formatter: function (val) {
          return val.toString()
        },
      },
      y: {
        formatter: function (val) {
          return Math.abs(val) + '%'
        },
      },
    },
    xaxis: {
      categories: [
        '90+',
        '85-89',
        '80-84',
        '75-79',
        '70-74',
        '65-69',
        '60-64',
        '55-59',
        '50-54',
        '45-49',
        '40-44',
        '35-39',
        '30-34',
        '25-29',
        '20-24',
        '15-19',
        '10-14',
        '0-9',
      ],
      title: {
        text: 'Percent',
      },
      labels: {
        formatter: function (val) {
          return Math.abs(Math.round(Number(val))) + '%'
        },
      },
    },
    legend: {
      offsetY: 7,
    },
  }

  reversedChart: Partial<ChartOptions> = {
    series: [
      {
        data: [380, 400, 418, 440, 500, 530, 580],
      },
    ],
    chart: {
      type: 'bar',
      height: 380,
      toolbar: {
        show: false,
      },
    },
    annotations: {
      xaxis: [
        {
          x: 500,
          borderColor: '#f9b931',
          label: {
            borderColor: '#f9b931',
            style: {
              color: '#fff',
              background: '#f9b931',
            },
            text: 'X annotation',
          },
        },
      ],
      yaxis: [
        {
          y: 'July',
          y2: 'September',
          label: {
            text: 'Y annotation',
          },
        },
      ],
    },
    plotOptions: {
      bar: {
        horizontal: true,
      },
    },
    dataLabels: {
      enabled: true,
    },
    xaxis: {
      categories: ['Jul', 'Aug', 'Sept', 'Oct', 'Nov', 'Dec', 'Jan'],
    },
    colors: ['#f9b931'],
    grid: {
      xaxis: {
        lines: {
          show: true,
        },
      },
    },
    yaxis: {
      reversed: true,
      axisTicks: {
        show: true,
      },
    },
  }

  imagefillChart: Partial<ChartOptions> = {
    chart: {
      height: 450,
      type: 'bar',
      toolbar: {
        show: false,
      },
      animations: {
        enabled: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
        barHeight: '100%',
      },
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      colors: ['#fff'],
      width: 0.2,
    },
    series: [
      {
        name: 'coins',
        data: [
          2, 4, 3, 4, 3, 5, 5, 6.5, 6, 5, 4, 5, 8, 7, 7, 8, 8, 10, 9, 9, 12, 12,
          11, 12, 13, 14, 16, 14, 15, 17, 19, 21,
        ],
      },
    ],
    yaxis: {
      axisBorder: {
        show: false,
      },
      axisTicks: {
        show: false,
      },
      labels: {
        show: false,
      },
      title: {
        text: 'Weight',
      },
    },
    grid: {
      position: 'back',
      borderColor: '#f1f3fa',
    },
    fill: {
      type: 'image',
      opacity: 0.87,
      image: {
        src: ['../../../assets/images/small/img-4.jpg'],
        width: 466,
        height: 406,
      },
    },
  }

  datalablesChart: Partial<ChartOptions> = {
    chart: {
      height: 450,
      type: 'bar',
    },
    plotOptions: {
      bar: {
        barHeight: '100%',
        distributed: true,
        horizontal: true,
        dataLabels: {
          position: 'bottom',
        },
      },
    },
    colors: [
      '#1c84ee',
      '#53389f',
      '#7f56da',
      '#ff86c8',
      '#ef5f5f',
      '#ff6c2f',
      '#f9b931',
      '#22c55e',
      '#040505',
      '#4ecac2',
    ],
    dataLabels: {
      enabled: true,
      textAnchor: 'start',
      style: {
        colors: ['#fff'],
      },
      formatter: function (val, opt) {
        return opt.w.globals.labels[opt.dataPointIndex] + ':  ' + val
      },
      offsetX: 0,
      dropShadow: {
        enabled: false,
      },
    },
    series: [
      {
        data: [400, 430, 448, 470, 540, 580, 690, 1100, 1200, 1380],
      },
    ],
    stroke: {
      width: 0,
      colors: ['#fff'],
    },
    xaxis: {
      categories: [
        'South Korea',
        'Canada',
        'United Kingdom',
        'Netherlands',
        'Italy',
        'France',
        'Japan',
        'United States',
        'China',
        'India',
      ],
      axisBorder: {
        show: false,
      },
    },
    yaxis: {
      labels: {
        show: false,
      },
    },
    grid: {
      borderColor: '#f1f3fa',
    },

    tooltip: {
      theme: 'dark',
      x: {
        show: false,
      },
      y: {
        title: {
          formatter: function () {
            return ''
          },
        },
      },
    },
  }

  patternChart: Partial<ChartOptions> = {
    chart: {
      height: 380,
      type: 'bar',
      stacked: true,
      toolbar: {
        show: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
        barHeight: '60%',
      },
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      width: 0,
    },
    series: [
      {
        name: 'Marine Sprite',
        data: [44, 55, 41, 37, 22, 43, 21],
      },
      {
        name: 'Striking Calf',
        data: [53, 32, 33, 52, 13, 43, 32],
      },
      {
        name: 'Tank Picture',
        data: [12, 17, 11, 9, 15, 11, 20],
      },
      {
        name: 'Bucket Slope',
        data: [9, 7, 5, 8, 6, 9, 4],
      },
    ],
    xaxis: {
      categories: [2008, 2009, 2010, 2011, 2012, 2013, 2014],
    },
    yaxis: {
      title: {
        text: undefined,
      },
    },
    tooltip: {
      shared: false,
      y: {
        formatter: function (val) {
          return val + 'K'
        },
      },
    },
    colors: ['#1c84ee', '#22c55e', '#040505', '#4ecac2'],
    fill: {
      type: 'pattern',
      opacity: 1,
      pattern: {
        style: ['circles', 'slantedLines', 'verticalLines', 'horizontalLines'], // string or array of strings
      },
    },
    states: {
      hover: {
        filter: { type: 'none' },
      },
    },
    legend: {
      position: 'right',
    },
    grid: {
      borderColor: '#f1f3fa',
    },
    responsive: [
      {
        breakpoint: 600,
        options: {
          legend: {
            show: false,
          },
        },
      },
    ],
  }

  markerChart: Partial<ChartOptions> = {
    series: [
      {
        name: 'Actual',
        data: [
          {
            x: '2017',
            y: 12,
            goals: [
              {
                name: 'Expected',
                value: 14,
                strokeWidth: 2,
                strokeDashArray: 2,
                strokeColor: '#22c55e',
              },
            ],
          },
          {
            x: '2018',
            y: 44,
            goals: [
              {
                name: 'Expected',
                value: 54,
                strokeWidth: 5,
                strokeHeight: 10,
                strokeColor: '#22c55e',
              },
            ],
          },
          {
            x: '2019',
            y: 54,
            goals: [
              {
                name: 'Expected',
                value: 52,
                strokeWidth: 10,
                strokeHeight: 0,
                strokeLineCap: 'round',
                strokeColor: '#22c55e',
              },
            ],
          },
          {
            x: '2020',
            y: 66,
            goals: [
              {
                name: 'Expected',
                value: 61,
                strokeWidth: 10,
                strokeHeight: 0,
                strokeLineCap: 'round',
                strokeColor: '#22c55e',
              },
            ],
          },
          {
            x: '2021',
            y: 81,
            goals: [
              {
                name: 'Expected',
                value: 66,
                strokeWidth: 10,
                strokeHeight: 0,
                strokeLineCap: 'round',
                strokeColor: '#22c55e1',
              },
            ],
          },
          {
            x: '2022',
            y: 67,
            goals: [
              {
                name: 'Expected',
                value: 70,
                strokeWidth: 5,
                strokeHeight: 10,
                strokeColor: '#22c55e',
              },
            ],
          },
        ],
      },
    ],
    chart: {
      height: 380,
      type: 'bar',
      toolbar: {
        show: false,
      },
    },
    plotOptions: {
      bar: {
        horizontal: true,
      },
    },
    colors: ['#f9b931', '#22c55e'],
    dataLabels: {},
    legend: {
      show: true,
      showForSingleSeries: true,
      customLegendItems: ['Actual', 'Expected'],
    },
    xaxis: {
      axisBorder: {
        show: false,
      },
    },
  }
}

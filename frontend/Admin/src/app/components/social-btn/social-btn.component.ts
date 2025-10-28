import { Component } from '@angular/core'

@Component({
  selector: 'app-social-btn',
  standalone: true,
  imports: [],
  template: `
    <p class="mt-3 fw-semibold no-span">OR sign with</p>

    <div class="text-center">
      <a href="javascript:void(0);" class="btn btn-light shadow-none me-1"
        ><i class="bx bxl-google fs-20"></i
      ></a>
      <a href="javascript:void(0);" class="btn btn-light shadow-none me-1"
        ><i class="bx bxl-facebook fs-20"></i
      ></a>
      <a href="javascript:void(0);" class="btn btn-light shadow-none"
        ><i class="bx bxl-github fs-20"></i
      ></a>
    </div>
  `,
  styles: ``,
})
export class SocialBtnComponent {}

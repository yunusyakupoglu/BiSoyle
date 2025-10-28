import { PageTitleComponent } from '@/app/components/page-title.component'
import { Component } from '@angular/core'
import { NgbNavModule } from '@ng-bootstrap/ng-bootstrap'
import { GeneralDEtailComponent } from './components/general-detail/general-detail.component'
import { MetadataComponent } from './components/metadata/metadata.component'
import { FinishComponent } from './components/finish/finish.component'
import { FileUploaderComponent } from '@/app/components/file-uploader'

@Component({
  selector: 'app-create',
  standalone: true,
  imports: [
    PageTitleComponent,
    NgbNavModule,
    GeneralDEtailComponent,
    MetadataComponent,
    FinishComponent,
    FileUploaderComponent,
  ],
  templateUrl: './create.component.html',
})
export class CreateComponent {
  active = 1
  active1 = 1
}

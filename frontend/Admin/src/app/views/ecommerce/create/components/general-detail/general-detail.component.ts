import { SelectFormInputDirective } from '@/app/directive/select-form-input.directive'
import { Component, inject } from '@angular/core'
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  type UntypedFormGroup,
} from '@angular/forms'
import { QuillModule } from 'ngx-quill'

@Component({
  selector: 'create-general-detail',
  standalone: true,
  imports: [
    QuillModule,
    FormsModule,
    ReactiveFormsModule,
    SelectFormInputDirective,
  ],
  templateUrl: './general-detail.component.html',
  styles: ``,
})
export class GeneralDEtailComponent {
  generalForm!: UntypedFormGroup

  private fb = inject(UntypedFormBuilder)

  ngOnInit(): void {
    this.generalForm = this.fb.group({
      content: ['Describe Your Product Description...'],
    })
  }

  editorConfig = {
    toolbar: [
      [{ font: [] }, { size: [] }],
      ['bold', 'italic', 'underline', 'strike'],
      [{ color: [] }, { background: [] }],
      [{ script: 'super' }, { script: 'sub' }],
      [{ header: [false, 1, 2, 3, 4, 5, 6] }, 'blockquote', 'code-block'],
      [
        { list: 'ordered' },
        { list: 'bullet' },
        { indent: '-1' },
        { indent: '+1' },
      ],
      ['direction', { align: [] }],
      ['link', 'image', 'video'],
      ['clean'],
    ],
  }
}

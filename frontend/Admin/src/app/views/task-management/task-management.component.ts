import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

interface Task {
  id: number;
  tenantId: number;
  createdByUserId: number;
  assignedToUserId: number;
  title: string;
  description?: string;
  status: string; // Pending, Accepted, InProgress, Completed
  priority: string; // Low, Medium, High, Critical
  dueDate?: string;
  createdDate: string;
  updatedDate: string;
  comments?: TaskComment[];
}

interface TaskComment {
  id: number;
  taskId: number;
  userId: number;
  comment: string;
  createdDate: string;
}

interface User {
  id: number;
  username: string;
  firstName?: string;
  lastName?: string;
  roles?: string[];
}

@Component({
  selector: 'app-task-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './task-management.component.html'
})
export class TaskManagementComponent implements OnInit {
  tasks: Task[] = [];
  users: User[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showTaskModal = false;
  showCommentModal = false;
  editingTask: Task | null = null;
  selectedTaskForComment: Task | null = null;
  saving = false;
  
  // Form data
  formData = {
    title: '',
    description: '',
    assignedToUserId: null as number | null,
    priority: 'Medium',
    dueDate: ''
  };
  
  commentText = '';
  
  // Status columns for Kanban board
  statusColumns = [
    { key: 'Pending', label: 'Beklemede', color: 'secondary' },
    { key: 'Accepted', label: 'Kabul Edildi', color: 'info' },
    { key: 'InProgress', label: 'Devam Ediyor', color: 'warning' },
    { key: 'Completed', label: 'Tamamlandı', color: 'success' }
  ];
  
  priorityColors: Record<string, string> = {
    'Low': 'bg-secondary',
    'Medium': 'bg-info',
    'High': 'bg-warning',
    'Critical': 'bg-danger'
  };
  
  currentUser: any = null;
  isAdmin = false;
  isUser = false;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.currentUser = this.authService.getUser();
    const roles = this.currentUser?.roles || [];
    this.isAdmin = roles.includes('Admin') || roles.includes('SuperAdmin');
    this.isUser = roles.includes('User') && !this.isAdmin;
    
    // Önce kullanıcıları yükle, sonra görevleri yükle
    this.loadUsers();
  }

  get headers() {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  loadUsers() {
    const tenantId = this.currentUser?.tenantId;
    let url = `${environment.apiUrl}/users`;
    if (tenantId && !this.isSuperAdmin()) {
      url += `?tenantId=${tenantId}`;
    }
    
    this.http.get<User[]>(url, { headers: this.headers }).subscribe({
      next: (data) => {
        // Sadece Admin ve User rolündeki kullanıcıları göster
        this.users = data.filter(u => {
          const userRoles = u.roles || [];
          return userRoles.includes('Admin') || userRoles.includes('User');
        });
        // Kullanıcılar yüklendikten sonra görevleri yükle
        this.loadTasks();
      },
      error: (err) => {
        console.error('Kullanıcılar yüklenemedi:', err);
        // Hata olsa bile görevleri yükle
        this.loadTasks();
      }
    });
  }

  loadTasks() {
    this.loading = true;
    this.error = null;

    const tenantId = this.currentUser?.tenantId;
    const params = new URLSearchParams();
    
    if (tenantId && !this.isSuperAdmin()) {
      params.append('tenantId', tenantId.toString());
    }
    
    // User rolü ise sadece kendisine atanan taskları göster
    if (this.isUser && this.currentUser?.id) {
      params.append('assignedToUserId', this.currentUser.id.toString());
    }
    
    const qs = params.toString() ? `?${params.toString()}` : '';
    
    this.http.get<Task[]>(`${environment.apiUrl}/tasks${qs}`, { headers: this.headers }).subscribe({
      next: (data) => {
        this.tasks = data;
        // Görevlerdeki kullanıcı bilgilerini kontrol et ve eksik olanları çek
        this.loadMissingUserInfo(data);
        this.loading = false;
      },
      error: (err) => {
        console.error('Görevler yüklenemedi:', err);
        this.error = 'Görevler yüklenirken bir hata oluştu.';
        this.loading = false;
      }
    });
  }

  loadMissingUserInfo(tasks: Task[]): void {
    // Görevlerdeki tüm kullanıcı ID'lerini topla
    const userIds = new Set<number>();
    tasks.forEach(task => {
      if (task.assignedToUserId) userIds.add(task.assignedToUserId);
      if (task.createdByUserId) userIds.add(task.createdByUserId);
      if (task.comments) {
        task.comments.forEach(comment => {
          if (comment.userId) userIds.add(comment.userId);
        });
      }
    });

    // Eksik kullanıcıları çek
    userIds.forEach(userId => {
      if (!this.users.find(u => u.id === userId)) {
        this.http.get<User>(`${environment.apiUrl}/users/${userId}`, { headers: this.headers }).subscribe({
          next: (user) => {
            // Kullanıcıyı listeye ekle
            if (!this.users.find(u => u.id === user.id)) {
              this.users.push(user);
            }
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.warn(`Kullanıcı ${userId} bilgisi alınamadı:`, err);
          }
        });
      }
    });
  }

  isSuperAdmin(): boolean {
    const user = this.authService.getUser();
    if (!user) return false;
    const hasSuperAdminRole = user.roles && user.roles.includes('SuperAdmin');
    const isSuperAdminTenant = !user.tenantId;
    return hasSuperAdminRole || isSuperAdminTenant;
  }

  getTasksByStatus(status: string): Task[] {
    return this.tasks.filter(t => t.status === status);
  }

  getUserName(userId: number): string {
    const user = this.users.find(u => u.id === userId);
    if (!user) {
      // Kullanıcı henüz yüklenmemişse, ID göster ama arka planda yükle
      if (userId) {
        this.loadSingleUser(userId);
      }
      return `Kullanıcı ${userId}`;
    }
    // Önce ad soyad, yoksa username, yoksa ID göster
    const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim();
    return fullName || user.username || `Kullanıcı ${userId}`;
  }

  loadSingleUser(userId: number): void {
    // Zaten yükleniyorsa tekrar yükleme
    if (this.users.find(u => u.id === userId)) {
      return;
    }
    
    this.http.get<User>(`${environment.apiUrl}/users/${userId}`, { headers: this.headers }).subscribe({
      next: (user) => {
        if (!this.users.find(u => u.id === user.id)) {
          this.users.push(user);
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        console.warn(`Kullanıcı ${userId} bilgisi alınamadı:`, err);
      }
    });
  }

  openCreateModal(): void {
    this.editingTask = null;
    this.formData = {
      title: '',
      description: '',
      assignedToUserId: null,
      priority: 'Medium',
      dueDate: ''
    };
    this.showTaskModal = true;
  }

  openEditModal(task: Task): void {
    this.editingTask = task;
    this.formData = {
      title: task.title || '',
      description: task.description || '',
      assignedToUserId: task.assignedToUserId,
      priority: task.priority || 'Medium',
      dueDate: task.dueDate ? task.dueDate.split('T')[0] : ''
    };
    this.showTaskModal = true;
  }

  openCommentModal(task: Task): void {
    this.selectedTaskForComment = task;
    this.commentText = '';
    this.loadTaskComments(task.id);
    this.showCommentModal = true;
  }

  loadTaskComments(taskId: number) {
    this.http.get<TaskComment[]>(`${environment.apiUrl}/tasks/${taskId}/comments`, { headers: this.headers }).subscribe({
      next: (comments) => {
        if (this.selectedTaskForComment) {
          this.selectedTaskForComment.comments = comments;
        }
      },
      error: (err) => {
        console.error('Yorumlar yüklenemedi:', err);
      }
    });
  }

  closeTaskModal(): void {
    this.showTaskModal = false;
    this.editingTask = null;
  }

  closeCommentModal(): void {
    this.showCommentModal = false;
    this.selectedTaskForComment = null;
    this.commentText = '';
  }

  saveTask(): void {
    if (!this.formData.title || !this.formData.assignedToUserId) {
      alert('Başlık ve atanan kullanıcı zorunludur!');
      return;
    }

    this.saving = true;
    const taskData: any = {
      tenantId: this.currentUser?.tenantId || 1,
      createdByUserId: this.currentUser?.id || 1,
      assignedToUserId: this.formData.assignedToUserId,
      title: this.formData.title,
      description: this.formData.description,
      priority: this.formData.priority,
      status: 'Pending'
    };

    if (this.formData.dueDate) {
      taskData.dueDate = new Date(this.formData.dueDate).toISOString();
    }

    if (this.editingTask) {
      // Update
      this.http.put(`${environment.apiUrl}/tasks/${this.editingTask.id}`, taskData, { headers: this.headers }).subscribe({
        next: () => {
          this.saving = false;
          this.closeTaskModal();
          this.loadTasks();
        },
        error: (err) => {
          console.error('Görev güncellenemedi:', err);
          alert(err.error?.detail || 'Görev güncellenirken bir hata oluştu!');
          this.saving = false;
        }
      });
    } else {
      // Create
      this.http.post(`${environment.apiUrl}/tasks`, taskData, { headers: this.headers }).subscribe({
        next: () => {
          this.saving = false;
          this.closeTaskModal();
          this.loadTasks();
        },
        error: (err) => {
          console.error('Görev oluşturulamadı:', err);
          alert(err.error?.detail || 'Görev oluşturulurken bir hata oluştu!');
          this.saving = false;
        }
      });
    }
  }

  updateTaskStatus(task: Task, newStatus: string): void {
    this.http.patch(`${environment.apiUrl}/tasks/${task.id}/status`, { status: newStatus }, { headers: this.headers }).subscribe({
      next: () => {
        task.status = newStatus;
        task.updatedDate = new Date().toISOString();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Görev durumu güncellenemedi:', err);
        alert('Görev durumu güncellenirken bir hata oluştu!');
      }
    });
  }

  deleteTask(task: Task): void {
    if (!confirm(`${task.title} görevini silmek istediğinizden emin misiniz?`)) {
      return;
    }

    this.http.delete(`${environment.apiUrl}/tasks/${task.id}`, { headers: this.headers }).subscribe({
      next: () => {
        this.loadTasks();
      },
      error: (err) => {
        console.error('Görev silinemedi:', err);
        alert('Görev silinirken bir hata oluştu!');
      }
    });
  }

  addComment(): void {
    if (!this.commentText.trim() || !this.selectedTaskForComment) {
      return;
    }

    const commentData = {
      userId: this.currentUser?.id || 1,
      comment: this.commentText.trim()
    };

    this.http.post(`${environment.apiUrl}/tasks/${this.selectedTaskForComment.id}/comments`, commentData, { headers: this.headers }).subscribe({
      next: () => {
        this.commentText = '';
        this.loadTaskComments(this.selectedTaskForComment!.id);
      },
      error: (err) => {
        console.error('Yorum eklenemedi:', err);
        alert('Yorum eklenirken bir hata oluştu!');
      }
    });
  }

  deleteComment(commentId: number): void {
    if (!this.selectedTaskForComment) return;
    
    if (!confirm('Yorumu silmek istediğinizden emin misiniz?')) {
      return;
    }

    this.http.delete(`${environment.apiUrl}/tasks/${this.selectedTaskForComment.id}/comments/${commentId}`, { headers: this.headers }).subscribe({
      next: () => {
        this.loadTaskComments(this.selectedTaskForComment!.id);
      },
      error: (err) => {
        console.error('Yorum silinemedi:', err);
        alert('Yorum silinirken bir hata oluştu!');
      }
    });
  }

  canEditTask(task: Task): boolean {
    // Admin oluşturduğu veya atadığı taskları düzenleyebilir
    if (this.isAdmin) {
      return task.createdByUserId === this.currentUser?.id || task.assignedToUserId === this.currentUser?.id;
    }
    // User sadece kendisine atanan taskları düzenleyebilir
    return task.assignedToUserId === this.currentUser?.id;
  }

  canDeleteTask(task: Task): boolean {
    // Sadece oluşturan admin silebilir
    return this.isAdmin && task.createdByUserId === this.currentUser?.id;
  }
}


// Custom Popup System for EVDMS
(function() {
    'use strict';

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializePopupSystem);
    } else {
        initializePopupSystem();
    }

    function initializePopupSystem() {
        createPopupContainer();
        console.log('Popup system initialized');
    }

    // Create popup container if not exists
    function createPopupContainer() {
        if (document.getElementById('custom-popup-container')) {
            return;
        }

        const container = document.createElement('div');
        container.id = 'custom-popup-container';
        container.style.cssText = 'position: fixed; top: 0; left: 0; width: 100%; height: 100%; z-index: 10000; pointer-events: none;';
        document.body.appendChild(container);
        console.log('Popup container created');
    }

    // Show custom popup
    window.showPopup = function(options) {
        createPopupContainer();

        const {
            message = '',
            title = 'Thông báo',
            confirmText = 'OK',
            cancelText = 'Không',
            showCancel = false,
            onConfirm = null,
            onCancel = null,
            type = 'info' // info, success, warning, error
        } = options;

        // Create popup HTML
        const popup = document.createElement('div');
        popup.className = 'custom-popup-overlay';
        popup.style.cssText = 'position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0, 0, 0, 0.5); display: flex; align-items: center; justify-content: center; z-index: 10001; pointer-events: all;';
        popup.innerHTML = `
            <div class="custom-popup-modal ${type}" style="background: #FFFFFF; border-radius: 12px; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2); max-width: 450px; width: 90%; overflow: hidden;">
                <div class="custom-popup-header" style="padding: 1.25rem 1.5rem; background: #F8F9FA; border-bottom: 1px solid #DEE2E6;">
                    <h3 class="custom-popup-title" style="margin: 0; font-size: 1.125rem; font-weight: 600; color: #212529;">${title}</h3>
                </div>
                <div class="custom-popup-body" style="padding: 1.5rem;">
                    <p class="custom-popup-message" style="margin: 0; font-size: 1rem; color: #495057; line-height: 1.6; white-space: pre-wrap;">${message}</p>
                </div>
                <div class="custom-popup-footer" style="padding: 1rem 1.5rem; background: #F8F9FA; border-top: 1px solid #DEE2E6; display: flex; gap: 0.75rem; justify-content: flex-end;">
                    ${showCancel ? `<button class="custom-popup-btn custom-popup-btn-cancel" style="padding: 0.625rem 1.5rem; border: 2px solid #6C757D; border-radius: 8px; font-size: 0.9375rem; font-weight: 600; cursor: pointer; min-width: 80px; background: #6C757D; color: #FFFFFF;">${cancelText}</button>` : ''}
                    <button class="custom-popup-btn custom-popup-btn-confirm" style="padding: 0.625rem 1.5rem; border: 2px solid #198754; border-radius: 8px; font-size: 0.9375rem; font-weight: 600; cursor: pointer; min-width: 80px; background: #198754; color: #FFFFFF;">${confirmText}</button>
                </div>
            </div>
        `;

        // Add to body directly (not to container)
        document.body.appendChild(popup);

        // Get buttons
        const confirmBtn = popup.querySelector('.custom-popup-btn-confirm');
        const cancelBtn = popup.querySelector('.custom-popup-btn-cancel');

        // Add hover effects
        confirmBtn.addEventListener('mouseenter', function() {
            this.style.background = '#157347';
            this.style.borderColor = '#157347';
            this.style.transform = 'translateY(-1px)';
            this.style.boxShadow = '0 4px 8px rgba(25, 135, 84, 0.3)';
        });
        confirmBtn.addEventListener('mouseleave', function() {
            this.style.background = '#198754';
            this.style.borderColor = '#198754';
            this.style.transform = 'translateY(0)';
            this.style.boxShadow = 'none';
        });

        if (cancelBtn) {
            cancelBtn.addEventListener('mouseenter', function() {
                this.style.background = '#5A6268';
                this.style.borderColor = '#5A6268';
                this.style.transform = 'translateY(-1px)';
            });
            cancelBtn.addEventListener('mouseleave', function() {
                this.style.background = '#6C757D';
                this.style.borderColor = '#6C757D';
                this.style.transform = 'translateY(0)';
            });
        }

        // Handle confirm
        confirmBtn.addEventListener('click', function() {
            if (onConfirm) {
                onConfirm();
            }
            closePopup(popup);
        });

        // Handle cancel
        if (cancelBtn) {
            cancelBtn.addEventListener('click', function() {
                if (onCancel) {
                    onCancel();
                }
                closePopup(popup);
            });
        }

        // Show popup with animation
        setTimeout(() => {
            popup.style.opacity = '0';
            popup.style.transition = 'opacity 0.3s ease';
            setTimeout(() => {
                popup.style.opacity = '1';
            }, 10);
        }, 10);

        // Close on overlay click
        popup.addEventListener('click', function(e) {
            if (e.target === popup) {
                if (onCancel) onCancel();
                closePopup(popup);
            }
        });

        // Handle ESC key
        const escHandler = function(e) {
            if (e.key === 'Escape') {
                if (onCancel) onCancel();
                closePopup(popup);
                document.removeEventListener('keydown', escHandler);
            }
        };
        document.addEventListener('keydown', escHandler);
    };

    // Close popup
    function closePopup(popup) {
        popup.style.opacity = '0';
        setTimeout(() => {
            popup.remove();
        }, 300);
    }

    // Alert replacement
    window.customAlert = function(message, title = 'Thông báo') {
        return new Promise((resolve) => {
            showPopup({
                message: message,
                title: title,
                confirmText: 'OK',
                showCancel: false,
                type: 'info',
                onConfirm: resolve
            });
        });
    };

    // Confirm replacement
    window.customConfirm = function(message, title = 'Xác nh?n') {
        return new Promise((resolve) => {
            showPopup({
                message: message,
                title: title,
                confirmText: 'OK',
                cancelText: 'Không',
                showCancel: true,
                type: 'warning',
                onConfirm: () => resolve(true),
                onCancel: () => resolve(false)
            });
        });
    };

    // Success message
    window.showSuccess = function(message, title = 'Thành công') {
        return new Promise((resolve) => {
            showPopup({
                message: message,
                title: title,
                confirmText: 'OK',
                showCancel: false,
                type: 'success',
                onConfirm: resolve
            });
        });
    };

    // Error message
    window.showError = function(message, title = 'L?i') {
        return new Promise((resolve) => {
            showPopup({
                message: message,
                title: title,
                confirmText: 'OK',
                showCancel: false,
                type: 'error',
                onConfirm: resolve
            });
        });
    };

    // Warning message
    window.showWarning = function(message, title = 'C?nh báo') {
        return new Promise((resolve) => {
            showPopup({
                message: message,
                title: title,
                confirmText: 'OK',
                showCancel: false,
                type: 'warning',
                onConfirm: resolve
            });
        });
    };

    console.log('Popup functions loaded: customConfirm, customAlert, showSuccess, showError, showWarning');
})();

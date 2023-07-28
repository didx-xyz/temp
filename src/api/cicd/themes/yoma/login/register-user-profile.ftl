<#import "template.ftl" as layout>
	<#import "user-profile-commons.ftl" as userProfileCommons>
		<@layout.registrationLayout displayMessage=messagesPerField.exists('global') displayRequiredFields=false; section>
			<#if section="header">
				${msg("registerTitle")}
				<#elseif section="form">
					<div class="centered-subTitles">
						<label>
							${msg("registerSubTitle")}
						</label>
					</div>
					<form id="kc-register-form" class="${properties.kcFormClass!}" action="${url.registrationAction}" method="post">
						<@userProfileCommons.userProfileFormFields; callback, attribute>
							<#if callback="afterField">
								<#-- render password fields just under the username or email (if used as username) -->
									<#if passwordRequired?? && (attribute.name=='username' || (attribute.name=='email' && realm.registrationEmailAsUsername))>
										<div class="${properties.kcFormGroupClass!}">
											<div class="${properties.kcLabelWrapperClass!}">
												<label for="password" class="${properties.kcLabelClass!}">
													${msg("password")}
												</label> *
											</div>
											<div class="${properties.kcInputWrapperClass!}">
												<input type="password" id="password" class="${properties.kcInputClass!}" name="password"
													autocomplete="new-password"
													aria-invalid="<#if messagesPerField.existsError('password','password-confirm')>true</#if>" />
												<#if messagesPerField.existsError('password')>
													<span id="input-error-password" class="${properties.kcInputErrorMessageClass!}" aria-live="polite">
														${kcSanitize(messagesPerField.get('password'))?no_esc}
													</span>
												</#if>
											</div>
										</div>
										<div class="${properties.kcFormGroupClass!}">
											<div class="${properties.kcLabelWrapperClass!}">
												<label for="password-confirm"
													class="${properties.kcLabelClass!}">
													${msg("passwordConfirm")}
												</label> *
											</div>
											<div class="${properties.kcInputWrapperClass!}">
												<input type="password" id="password-confirm" class="${properties.kcInputClass!}"
													name="password-confirm"
													aria-invalid="<#if messagesPerField.existsError('password-confirm')>true</#if>" />
												<#if messagesPerField.existsError('password-confirm')>
													<span id="input-error-password-confirm" class="${properties.kcInputErrorMessageClass!}" aria-live="polite">
														${kcSanitize(messagesPerField.get('password-confirm'))?no_esc}
													</span>
												</#if>
											</div>
										</div>
									</#if>
							</#if>
							</@userProfileCommons.userProfileFormFields>
							<!-- other inputs -->
							<div class="centered-div">
								<div class="centered-checkbox">
									<input
										type="checkbox"
										id="terms"
										name="user.attributes.terms_and_conditions"
										value="Yes"
										required />
									<label for="terms">
										<a href="https://app.yoma.world/terms-and-conditions" target="_blank">
											I agree to yoma’s privacy policy and terms of service
										</a>
									</label>
								</div>
							</div>
							<#if recaptchaRequired??>
								<div class="form-group">
									<div class="${properties.kcInputWrapperClass!}">
										<div class="g-recaptcha" data-size="compact" data-sitekey="${recaptchaSiteKey}"></div>
									</div>
								</div>
							</#if>
							<div id="kc-form-buttons" class="${properties.kcFormButtonsClass!}">
								<input class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!} ${properties.kcButtonBlockClass!} ${properties.kcButtonLargeClass!}" type="submit" value="${msg("doRegister")}" />
							</div>
							<div class="${properties.kcFormGroupClass!}">
								<div id="kc-form-options" class="${properties.kcFormOptionsClass!}">
									<div style="display: inline;" class="${properties.kcFormOptionsWrapperClass!}">
										<hr class="grey-hr">
										<span>
											<a href="${url.loginUrl}">
												${msg("verifiedAccount")}
												${kcSanitize(msg("backToLogin"))?no_esc}
											</a></span>
									</div>
								</div>
							</div>
					</form>
			</#if>
			
		</@layout.registrationLayout>
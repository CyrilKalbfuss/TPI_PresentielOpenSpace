   M S B A M L   `   `   `  ����E  Apresentiel, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:presentiel
presentiel  5   	   _ [Microsoft.Lync.Controls, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429cbFclr-namespace:Microsoft.Lync.Controls;assembly=Microsoft.Lync.ControlsMicrosoft.Lync.Controls 5   	   =#clr-namespace:presentiel.Propertiespresentiel.Properties  5   	       presentiel.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:presentiel     5   	   [controlsFclr-namespace:Microsoft.Lync.Controls;assembly=Microsoft.Lync.Controls     5   	   ;appSettings#clr-namespace:presentiel.Properties     5   	   -   5          �Name$  presentielWindow��5   a    � Title$ 
MainWindow��5
   	   $	��158��6   $	��263��6)   
������� � 	Resources 5         USBLogo �� 	UriSource 
 BTLogo  propertiesLogo��%�   &	        &	 1     &	 h     �� 5   
   $ ./resources/usbLogo.png?�6&   �� 5   
   $# ./resources/bluetoothLogo.png?�6%   �� 5   
   $$ ./resources/propertiesGear.png?�6-   5      .��5      � -   5         �Name$ MainView��5      .+�5   
   *  $Microsoft.Lync.Controls.MyStatusArea  $
��Left=�6    $��	10,10,0,0q�6;   $	��Top=�6N   �� 5   
   -   5         ��Name$ CmbAppPresence��5      $
��Left=�6+   $��	79,58,0,0q�6F   $	��Top=�6Y   $	��103��6q   $��25��6}   
�������
�������
���   �6�   �� 5   
   -   5         ��Name$ lblSkypeDisconnected��5      $
��Left=�62   $��	10,10,0,0q�6M   $
��Wrap=�6`   $ ��Skype n'est pas connecté.��6t   $	��Top=�6�   $	��172��6�   
����������	#FFDA0000$��25��6�   �� 5   
   -   5        $ lblNoConnection��5      $
��Left=�6-   $��	11,97,0,0q�6H   $
��Wrap=�6[   $%��Le boitier n'est pas connecté.��6o   $	��Top=�6�   ���	#FFDA0000 � Cursor$
 Hand��6�   6�   �� 5   
   -   5         ��Name$ ColorAppPresence��5      
����  �$
��Left=�6:   $��50��6U   $��	11,40,0,0q�6a   $	��Top=�6t   $��9��6�   �� 5   
   -   5        	 ��Name$	 btnComSwitch��5      $�� ��6'   $
��Left=�62   $��
210,10,0,0q�6M   $	��Top=�6a   $��35��6y   $��35��6�   ��5      �� 5      
 �� ImageSource
 6   ��  �� Mode$ OneWay=� �� Source# [  5      �� 5   
   -   5        $	 
btnOptions��5      $�� ��6%   $
��Left=�60   $��
210,75,0,0q�6K   $	��Top=�6_   $��35��6w   $��35��6�    J� RenderTransformOrigin$ 0.485,-1.433&�6�   ��5      �� 5      #
 �  6   5!      � 5$   
   -	   5        $ OptionsView��5$      
�������$y�Hidden=�67   .+�5%      �� $��Options��6   $
��Left=�6&   $��10,0,0,0q�6A   $	��Top=�6S   $��14(�6k   $
��Bold"�6y   �� 5&      -
   5        $ 	lblComUsb��5&      $
��Left=�6+   $��	10,34,0,0q�6F   $
��Wrap=�6Y   $��Port COM USB:��6m   $	��Top=�6�   �� 5'      $
��Left=�6c   $��23��6~   $��
143,31,0,0q�6�    �� TextWrapping$
 Wrap=�6�   $	��Top=�6�   $��79��6�   ��6   �� $  presentiel.Properties.Settings   Default# Z  �� Path$ UsbCOM�6�   �� 5(      $
��Left=�6   $��	10,62,0,0q�63   $
��Wrap=�6F   $��Port COM Bluetooth:��6Z   $	��Top=�6u   �� 5)      $
��Left=�6i   $��23��6�   $��
143,59,0,0q�6�   $
 Wrap=�6�   $	��Top=�6�   $��79��6�   ��6   �� # Z $ BluetoothCOM�6�   �� 5*      -   5        $	 BtnCloseOptions��5*      $��Fermer��6.   $
��Left=�6?   $��	71,91,0,0q�6Z   $	��Top=�6m   $��75��6�   6�   5+      � 5.   
   -   5        $ connectionErrorView��5.      
�������$y�Hidden=�6?   .+�5/      �� -   5         ��Name$ lblConnecionErrorTitle��5/      $$��Erreur de connexion au boitier��64   $
��Left=�6]   $��10,0,0,0q�6x   $	��Top=�6�   $��14(�6�   $
��Bold"�6�   ���	#FFDA0000��6�   Z� �� 50      -   5        $	 btnConnectionError��50      $��Fermer��61   $
��Left=�6B   $��	85,92,0,0q�6]   $	��Top=�6p   $��75��6�   6�   �� 51      -   5        $ lblConnecionError1��51      $
��Left=�64   $
��Wrap=�6O   $0��*Vérifiez que le boitier soit fonctionnel.��6c   $	��Top=�6�   $��	10,29,0,0q�6�   $	��235��6�   �� 52      -   5        $ lblConnecionError2��52      $
��Left=�64   $��	10,50,0,0q�6O   $
��Wrap=�6b   $0��*Vérifiez le mode de communication choisi.��6v   $	��Top=�6�   $	��235��6�   �� 53      -   5        $ lblConnecionError3��53      $
��Left=�64   $��	10,71,0,0q�6O   $
��Wrap=�6b   $(��"Vérifiez les options de port COM.��6v   $	��Top=�6�   $	��235��6�   54      56      57      
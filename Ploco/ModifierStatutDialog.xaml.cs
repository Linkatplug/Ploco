﻿using System;
using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco
{
    public partial class ModifierStatutDialog : Window
    {
        public StatutLocomotive NewStatut { get; private set; }

        public ModifierStatutDialog(Locomotive loco)
        {
            InitializeComponent();
            tbLocoInfo.Text = loco.ToString();
            // Pré-remplissage des champs date/heure avec la date/heure actuelles.
            tbEMDateTime.Text = DateTime.Now.ToString("G");
            tbATEVAPDateTime.Text = DateTime.Now.ToString("G");

            // Sélection par défaut dans le ComboBox en fonction du statut actuel.
            cbStatut.SelectedIndex = (int)loco.Statut;
        }

        private void btnEMModifier_Click(object sender, RoutedEventArgs e)
        {
            tbEMDateTime.IsReadOnly = !tbEMDateTime.IsReadOnly;
            if (!tbEMDateTime.IsReadOnly)
            {
                tbEMDateTime.Focus();
                tbEMDateTime.SelectAll();
            }
        }

        private void btnATEVAPModifier_Click(object sender, RoutedEventArgs e)
        {
            tbATEVAPDateTime.IsReadOnly = !tbATEVAPDateTime.IsReadOnly;
            if (!tbATEVAPDateTime.IsReadOnly)
            {
                tbATEVAPDateTime.Focus();
                tbATEVAPDateTime.SelectAll();
            }
        }

        private void btnValider_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer le nouveau statut depuis le ComboBox.
            if (cbStatut.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string statutString = selectedItem.Tag.ToString();
                if (Enum.TryParse(statutString, out StatutLocomotive statut))
                {
                    NewStatut = statut;
                }
            }

            string defautMoteur = "";
            if (chkDefautMoteur.IsChecked == true)
            {
                defautMoteur = "Défaut moteur: ";
                if (chk75.IsChecked == true) defautMoteur += "75% ";
                if (chk50.IsChecked == true) defautMoteur += "50% ";
                if (chkPRP1.IsChecked == true) defautMoteur += "PRP1 ";
                if (chkPRP2.IsChecked == true) defautMoteur += "PRP2 ";
                if (chkCVS1.IsChecked == true) defautMoteur += "CVS1 ";
                if (chkCVS2.IsChecked == true) defautMoteur += "CVS2 ";
            }

            string emInfo = "";
            if (chkEM.IsChecked == true)
            {
                emInfo = $"EM: Date/Heure={tbEMDateTime.Text}, Note={tbEMNote.Text}";
            }

            string atevapInfo = "";
            if (chkATEVAP.IsChecked == true)
            {
                atevapInfo = $"ATE/VAP: Date/Heure={tbATEVAPDateTime.Text}, Note={tbATEVAPNote.Text}";
            }

            string notesLibres = tbNotesLibres.Text;
            string archiveEntry = $"Action: Modifier Statut, Loco: {tbLocoInfo.Text}, Nouveau Statut: {NewStatut}, {defautMoteur}, {emInfo}, {atevapInfo}, Notes: {notesLibres}, Créé le: {DateTime.Now:G}";

            try
            {
                System.IO.File.AppendAllText("StatutModificationLog.txt", archiveEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement de l'archive : " + ex.Message);
            }

            this.DialogResult = true;
            this.Close();
        }

        private void btnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void chkDefautMoteur_Checked(object sender, RoutedEventArgs e)
        {
            spDefautMoteurDetails.Visibility = Visibility.Visible;
        }

        private void chkDefautMoteur_Unchecked(object sender, RoutedEventArgs e)
        {
            spDefautMoteurDetails.Visibility = Visibility.Collapsed;
        }

        private void chkEM_Checked(object sender, RoutedEventArgs e)
        {
            spEMDetails.Visibility = Visibility.Visible;
        }

        private void chkEM_Unchecked(object sender, RoutedEventArgs e)
        {
            spEMDetails.Visibility = Visibility.Collapsed;
        }

        private void chkATEVAP_Checked(object sender, RoutedEventArgs e)
        {
            spATEVAPDetails.Visibility = Visibility.Visible;
        }

        private void chkATEVAP_Unchecked(object sender, RoutedEventArgs e)
        {
            spATEVAPDetails.Visibility = Visibility.Collapsed;
        }
    }
}
